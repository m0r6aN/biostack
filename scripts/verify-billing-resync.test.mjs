import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluateQuarantine, evaluateResync, evaluateSubscription, main } from './verify-billing-resync.mjs';

test('evaluateResync fails when the summary contract is incomplete', () => {
  assert.equal(evaluateResync(null).status, 'failed');
  assert.equal(evaluateResync({ reconciled: 1 }).status, 'failed');
  assert.equal(evaluateResync({ reconciled: 1, unmappedCustomerIds: [], unresolvedPriceIds: 'x' }).status, 'failed');
  assert.equal(evaluateResync({ reconciled: -1, unmappedCustomerIds: [], unresolvedPriceIds: [] }).status, 'failed');
});

test('evaluateResync reports findings for unmapped customers and unresolved prices', () => {
  const unmapped = evaluateResync({ reconciled: 2, unmappedCustomerIds: ['cus_lost'], unresolvedPriceIds: [] });
  assert.equal(unmapped.status, 'findings');
  assert.match(unmapped.reason, /cus_lost/);

  const unresolved = evaluateResync({ reconciled: 2, unmappedCustomerIds: [], unresolvedPriceIds: ['price_new'] });
  assert.equal(unresolved.status, 'findings');
  assert.match(unresolved.reason, /Stripe:OperatorPriceId|price_new/);
});

test('evaluateResync passes on a clean reconciliation', () => {
  const evaluation = evaluateResync({ reconciled: 3, unmappedCustomerIds: [], unresolvedPriceIds: [] });
  assert.equal(evaluation.status, 'passed');
  assert.match(evaluation.reason, /3 subscription/);
});

test('evaluateQuarantine summarizes failure codes and passes when empty', () => {
  assert.equal(evaluateQuarantine([]).status, 'passed');
  assert.equal(evaluateQuarantine('nope').status, 'failed');

  const evaluation = evaluateQuarantine([
    { failureCode: 'unknown_stripe_price' },
    { failureCode: 'unknown_stripe_price' },
    { failureCode: 'unmapped_app_user' },
  ]);
  assert.equal(evaluation.status, 'findings');
  assert.match(evaluation.reason, /unknown_stripe_price×2/);
  assert.match(evaluation.reason, /unmapped_app_user×1/);
});

test('evaluateSubscription fails while the read model still shows Observer', () => {
  assert.equal(evaluateSubscription(null).status, 'failed');
  assert.equal(evaluateSubscription({ status: 'Active' }).status, 'failed');

  const stale = evaluateSubscription({ tier: 'Observer', status: 'Active', currentPeriodEndUtc: '2026-09-28T00:00:00Z' });
  assert.equal(stale.status, 'failed');
  assert.match(stale.reason, /Observer/);
});

test('evaluateSubscription passes for paid tiers with period details', () => {
  const operator = evaluateSubscription({ tier: 'Operator', status: 'Active', currentPeriodEndUtc: '2026-10-28T00:00:00Z' });
  assert.equal(operator.status, 'passed');
  assert.match(operator.reason, /Operator/);
  assert.match(operator.reason, /2026-10-28/);

  assert.equal(evaluateSubscription({ tier: 'Commander', status: 'Trialing' }).status, 'passed');
});

test('main enforces required environment and reports a failing subscription read model', async () => {
  const missing = await main({}, async () => {
    throw new Error('should not be called');
  });
  assert.equal(missing, 1);

  const calls = [];
  const fakeFetch = async (baseUrl, path, options) => {
    calls.push({ baseUrl, path, method: options?.method ?? 'GET' });
    if (path.includes('quarantined')) return { status: 200, body: [] };
    if (path.includes('resync')) return { status: 200, body: { reconciled: 1, unmappedCustomerIds: [], unresolvedPriceIds: [] } };
    return { status: 200, body: { tier: 'Observer', status: 'Active' } };
  };

  const exit = await main(
    {
      BIOSTACK_BASE_URL: 'https://staging.example/',
      BIOSTACK_ADMIN_COOKIE: 'session=admin',
      BIOSTACK_USER_COOKIE: 'session=user',
      BIOSTACK_CUSTOMER_ID: 'cus_example',
    },
    fakeFetch,
  );

  assert.equal(exit, 1);
  assert.equal(calls[0].baseUrl, 'https://staging.example');
  assert.match(calls[1].path, /resync\?customerId=cus_example/);
  assert.equal(calls[1].method, 'POST');
});

test('main passes end-to-end when resync reconciles and the read model is paid', async () => {
  const fakeFetch = async (_baseUrl, path) => {
    if (path.includes('quarantined')) return { status: 200, body: [{ failureCode: 'unmapped_app_user' }] };
    if (path.includes('resync')) return { status: 200, body: { reconciled: 4, unmappedCustomerIds: [], unresolvedPriceIds: [] } };
    return { status: 200, body: { tier: 'Operator', status: 'Active', currentPeriodEndUtc: '2026-10-28T00:00:00Z' } };
  };

  const exit = await main(
    {
      BIOSTACK_BASE_URL: 'https://staging.example',
      BIOSTACK_ADMIN_COOKIE: 'session=admin',
      BIOSTACK_USER_COOKIE: 'session=user',
    },
    fakeFetch,
  );

  // The quarantined-events listing is informational; the paid read model is the proof.
  assert.equal(exit, 2);
});
