import AuditReceiptFeedPage from "@/app/governance/receipts/page";
import { apiClient } from "@/lib/api";
import { render, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const useAuthMock = vi.fn();

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("@/lib/AuthProvider", () => ({
  useAuth: () => useAuthMock(),
}));

vi.mock("@/lib/flags", () => ({
  isEnabled: () => true,
}));

vi.mock("@/components/Header", () => ({
  Header: () => <header>Audit Receipt Feed</header>,
}));

vi.mock("@/components/governance/ReceiptDrawer", () => ({
  ReceiptDrawer: () => null,
}));

vi.mock("@/lib/api", () => ({
  apiClient: {
    getReceiptsByActor: vi.fn(),
    getReceiptsBySubject: vi.fn(),
  },
}));

describe("AuditReceiptFeedPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthMock.mockReturnValue({
      user: {
        id: "4d88ca43-a730-4a86-9f1c-d2e8d6c3db97",
        email: "observer@example.com",
        displayName: "Observer",
        role: 0,
      },
      loading: false,
    });
    vi.mocked(apiClient.getReceiptsByActor).mockResolvedValue([]);
  });

  it("requests only the signed-in user actor feed", async () => {
    render(<AuditReceiptFeedPage />);

    await waitFor(() => {
      expect(apiClient.getReceiptsByActor).toHaveBeenCalledWith(
        "user:4d88ca43-a730-4a86-9f1c-d2e8d6c3db97",
      );
    });
  });

  it("does not request receipts before authentication resolves", () => {
    useAuthMock.mockReturnValue({ user: null, loading: true });

    render(<AuditReceiptFeedPage />);

    expect(apiClient.getReceiptsByActor).not.toHaveBeenCalled();
    expect(apiClient.getReceiptsBySubject).not.toHaveBeenCalled();
  });
});
