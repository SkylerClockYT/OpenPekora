using Roblox.Models.Assets;
using Roblox.Models.Economy;

namespace Roblox.Dto.Economy;

public class EconomyTransactionBase
{
    public long userIdOne { get; set; }
    public long userIdTwo { get; set; }
    public PurchaseType type { get; set; }
    public TransactionSubType? subType { get; set; }
    public string? itemName { get; set; }
    public long amount { get; set; }
    public CurrencyType currencyType { get; set; }
    public long? groupIdOne { get; set; }
    public long? groupIdTwo { get; set; }
    public long? assetId { get; set; }
    public long? userAssetId { get; set; }
    public string? oldUsername { get; set; }
    public string? newUsername { get; set; }

    public EconomyTransactionBase()
    {

    }

    public void SetSelf(CreatorType type, long id)
    {
        // filter creatorType
        if (type == CreatorType.Group)
        {
            groupIdOne = id;
        }
        else if (type == CreatorType.User)
        {
            userIdOne = id;
        }
        else
        {
            throw new ArgumentException(nameof(type) + " is invalid: " + type);
        }
    }

    public void SetOther(CreatorType type, long id)
    {
        // filter creatorType
        if (type == CreatorType.Group)
        {
            groupIdTwo = id;
        }
        else if (type == CreatorType.User)
        {
            userIdTwo = id;
        }
        else
        {
            throw new ArgumentException(nameof(type) + " is invalid: " + type);
        }
    }
}

public interface IEconomyTransaction
{
    EconomyTransactionBase GetDto();
}

public class GroupFundRecipientTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public GroupFundRecipientTransaction(CreatorType creatorType, long creatorId, long groupId, CurrencyType currencyType, long amount)
    {
        transaction = new EconomyTransactionBase()
        {
            amount = amount,
            currencyType = currencyType,
            type = PurchaseType.GroupPayouts,
            subType = TransactionSubType.GroupPayoutReceived,
        };
        transaction.SetSelf(creatorType, creatorId);
        transaction.SetOther(CreatorType.Group, groupId);
    }
}

public class GroupFundPayoutTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public GroupFundPayoutTransaction(long groupId, CurrencyType currencyType, long amount, CreatorType recipientType, long recipientId)
    {
        transaction = new EconomyTransactionBase()
        {
            amount = amount,
            currencyType = currencyType,
            type = PurchaseType.GroupPayouts,
            subType = TransactionSubType.GroupPayoutSent,
        };
        transaction.SetSelf(CreatorType.Group, groupId);
        transaction.SetOther(recipientType, recipientId);
    }
}

public class AudioUploadTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public AudioUploadTransaction(CreatorType creatorType, long creatorId)
    {
        const long amount = 100;
        transaction = new EconomyTransactionBase()
        {
            amount = amount,
            currencyType = CurrencyType.Robux,
            type = PurchaseType.Purchase,
            subType = TransactionSubType.AudioUploadLong,
        };
        transaction.SetSelf(creatorType, creatorId);
        transaction.SetOther(CreatorType.User, 1);
    }
}

public class GameThumbnailUploadTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public GameThumbnailUploadTransaction(CreatorType creatorType, long creatorId)
    {
        transaction = new EconomyTransactionBase()
        {
            amount = 10,
            currencyType = CurrencyType.Robux,
            type = PurchaseType.Purchase,
            subType = TransactionSubType.GameMediaUpload,
        };
        transaction.SetSelf(creatorType, creatorId);
        transaction.SetOther(CreatorType.User, 1);
    }
}

public class AssetPurchaseTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public AssetPurchaseTransaction(long userIdPurchaser, CreatorType sellerType, long sellerId, CurrencyType currency, long amount, long assetId, long userAssetId)
    {
        transaction = new EconomyTransactionBase()
        {
            userIdOne = userIdPurchaser,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Purchase,
            subType = TransactionSubType.ItemPurchase,

            userAssetId = userAssetId,
            assetId = assetId,
        };
        transaction.SetOther(sellerType, sellerId);
    }
}

public class DevProdPurchaseTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public DevProdPurchaseTransaction(long userIdPurchaser, CreatorType sellerType, long sellerId, CurrencyType currency, long amount, string productName)
    {
        transaction = new EconomyTransactionBase
        {
            userIdOne = userIdPurchaser,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Purchase,
            itemName = productName,
        };
        transaction.SetOther(sellerType, sellerId);
    }
}

public class DevProdSaleTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public DevProdSaleTransaction(long userIdPurchaser, CreatorType sellerType, long sellerId, CurrencyType currency, long amount, string productName)
    {
        transaction = new EconomyTransactionBase
        {
            userIdTwo = userIdPurchaser,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Sale,
            itemName = productName,
        };
        transaction.SetSelf(sellerType, sellerId);
    }
}


public class AssetResalePurchaseTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public AssetResalePurchaseTransaction(long userIdPurchaser, long sellerId, CurrencyType currency, long amount, long assetId, long userAssetId)
    {
        transaction = new EconomyTransactionBase()
        {
            userIdOne = userIdPurchaser,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Purchase,
            subType = TransactionSubType.ItemResalePurchase,
            userIdTwo = sellerId,

            userAssetId = userAssetId,
            assetId = assetId,
        };
    }
}

public class AssetSaleTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public AssetSaleTransaction(long userIdPurchaser, CreatorType sellerType, long sellerId, CurrencyType currency, long amount, long assetId, long userAssetId)
    {
        transaction = new EconomyTransactionBase()
        {
            userIdTwo = userIdPurchaser,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Sale,
            subType = TransactionSubType.ItemSale,

            userAssetId = userAssetId,
            assetId = assetId,
        };
        transaction.SetSelf(sellerType, sellerId);
    }
}

public class AssetReSaleTransaction : IEconomyTransaction
{
    private EconomyTransactionBase transaction { get; set; }

    public EconomyTransactionBase GetDto()
    {
        return transaction;
    }

    public AssetReSaleTransaction(long userIdPurchaser, long userIdSeller, CurrencyType currency, long amount, long assetId, long userAssetId)
    {
        transaction = new EconomyTransactionBase()
        {
            userIdTwo = userIdPurchaser,
            userIdOne = userIdSeller,
            amount = amount,
            currencyType = currency,
            type = PurchaseType.Sale,
            subType = TransactionSubType.ItemResale,

            userAssetId = userAssetId,
            assetId = assetId,
        };
    }
}