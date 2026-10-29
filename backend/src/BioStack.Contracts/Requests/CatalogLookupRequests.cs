namespace BioStack.Contracts.Requests;

/// <summary>Ask whether a name the person typed is already in BioStack. <c>CompoundId</c> is their own item the answer is for.</summary>
public sealed record CreateCatalogLookupRequest(string? Name, Guid? CompoundId);

/// <summary>Confirm one of the offered candidates. The slug must be one the lookup itself offered.</summary>
public sealed record LinkCatalogLookupRequest(string? Slug);
