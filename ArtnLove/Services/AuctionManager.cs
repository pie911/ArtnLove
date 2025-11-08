using System.Collections.Concurrent;

namespace ArtnLove.Services;

/// <summary>
/// Lightweight in-memory auction manager for demo/tests.
/// Production: replace with DB-backed transactional implementation.
/// </summary>
public class AuctionManager
{
    private readonly ConcurrentDictionary<Guid, Auction> _auctions = new();

    public Auction CreateAuction(Guid artworkId, Guid ownerId, decimal startAmount, decimal minIncrement, DateTimeOffset endsAt)
    {
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ArtworkId = artworkId,
            OwnerId = ownerId,
            StartAmount = startAmount,
            MinIncrement = minIncrement,
            EndsAt = endsAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _auctions[auction.Id] = auction;
        return auction;
    }

    public bool TryGetAuction(Guid auctionId, out Auction? auction)
    {
        return _auctions.TryGetValue(auctionId, out auction);
    }

    public BidResult PlaceBid(Guid auctionId, Guid bidderId, decimal amount)
    {
        if (!_auctions.TryGetValue(auctionId, out var auction)) return BidResult.NotFound;

        if (auction.Settled) return BidResult.Settled;
        if (DateTimeOffset.UtcNow >= auction.EndsAt) return BidResult.Expired;

        var highest = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
        decimal minAllowed = highest?.Amount switch
        {
            null => auction.StartAmount,
            decimal h => h + auction.MinIncrement
        };

        if (amount < minAllowed) return BidResult.TooLow;

        var bid = new Bid { Id = Guid.NewGuid(), AuctionId = auctionId, BidderId = bidderId, Amount = amount, CreatedAt = DateTimeOffset.UtcNow };
        auction.Bids.Add(bid);
        return BidResult.Accepted;
    }
}

public class Auction
{
    public Guid Id { get; set; }
    public Guid ArtworkId { get; set; }
    public Guid OwnerId { get; set; }
    public decimal StartAmount { get; set; }
    public decimal MinIncrement { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool Settled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<Bid> Bids { get; set; } = new();
}

public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum BidResult
{
    NotFound,
    Settled,
    Expired,
    TooLow,
    Accepted
}
