using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace FieldKit.Server;

public record FieldKitServerMetadata : IModMetadata
{
    public string ModGuid { get; init; } =
        "com.hysocs.fieldkit";
    public string Name { get; init; } = "HysocsFieldKit";
    public string Author { get; init; } = "Hysocs";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } =
        new("1.2.0");
    public SemanticVersioning.Range SptVersion { get; init; } =
        new("~4.1.0");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>?
        ModDependencies { get; init; }
    public string? Url { get; init; } =
        "https://github.com/Hysocs/fieldkit-spt";
    public string License { get; init; } = "Apache-2.0";
}

[Injectable]
public sealed class FieldKitInventoryRouter(
    JsonUtil jsonUtil,
    FieldKitInventoryCallback callback)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<FieldKitAddItemRequest>(
                "/fieldkit/inventory/add",
                async (url, info, sessionId, output, cancellationToken) =>
                    await callback.AddItemToStash(
                        url,
                        info,
                        sessionId)),
            new RouteAction<FieldKitPrepareItemRequest>(
                "/fieldkit/inventory/prepare",
                async (url, info, sessionId, output, cancellationToken) =>
                    await callback.PrepareItem(
                        info,
                        sessionId)),
            new RouteAction<FieldKitCancelItemRequest>(
                "/fieldkit/inventory/cancel",
                async (url, info, sessionId, output, cancellationToken) =>
                    await callback.CancelPreparedItem(
                        info,
                        sessionId))
        ])
{
}

[Injectable]
public sealed class FieldKitInventoryCallback(
    ItemHelper itemHelper,
    ProfileHelper profileHelper,
    MailSendService mailSendService,
    HttpResponseUtil httpResponseUtil)
{
    private readonly HashSet<MongoId> _preparedItemIds = [];

    public async ValueTask<string> AddItemToStash(
        string url,
        FieldKitAddItemRequest request,
        MongoId sessionId)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            return httpResponseUtil.NoBody(
                FieldKitAddItemResponse.Failure(
                    "No item template was supplied."));
        }

        MongoId templateId;
        try
        {
            templateId = new MongoId(request.TemplateId);
        }
        catch
        {
            return httpResponseUtil.NoBody(
                FieldKitAddItemResponse.Failure(
                    "The item template ID is invalid."));
        }

        var templateResult = itemHelper.GetItem(templateId);
        if (!templateResult.Key || templateResult.Value is null)
        {
            return httpResponseUtil.NoBody(
                FieldKitAddItemResponse.Failure(
                    "That item template does not exist."));
        }

        int amount = Math.Clamp(request.Amount, 1, 100);
        var items = new List<Item>(amount);
        for (int index = 0; index < amount; index++)
        {
            var item = new Item
            {
                Id = CreateMongoId(),
                Template = templateId,
                Upd = itemHelper.GenerateUpdForItem(templateResult.Value)
            };
            item.Upd.StackObjectsCount ??= 1;
            items.Add(item);
        }

        mailSendService.SendSystemMessageToPlayer(
            sessionId,
            "FieldKit delivered the requested item.",
            items,
            604800,
            null);

        return httpResponseUtil.NoBody(
            new FieldKitAddItemResponse(
                true,
                false,
                "Item sent to your messages."));
    }

    public ValueTask<string> PrepareItem(
        FieldKitPrepareItemRequest request,
        MongoId sessionId)
    {
        if (!TryGetTemplate(
                request.TemplateId,
                out MongoId templateId,
                out TemplateItem? template,
                out string? error))
        {
            return ValueTask.FromResult(
                httpResponseUtil.NoBody(
                    FieldKitAddItemResponse.Failure(error!)));
        }

        MongoId itemId;
        try
        {
            itemId = new MongoId(request.ItemId);
        }
        catch
        {
            return ValueTask.FromResult(
                httpResponseUtil.NoBody(
                    FieldKitAddItemResponse.Failure(
                        "The item instance ID is invalid.")));
        }

        PmcData? profile = profileHelper.GetPmcProfile(sessionId);
        if (profile?.Inventory?.Items is null)
        {
            return ValueTask.FromResult(
                httpResponseUtil.NoBody(
                    FieldKitAddItemResponse.Failure(
                        "The active PMC profile could not be loaded.")));
        }

        if (profile.Inventory.Items.Any(item => item.Id == itemId))
        {
            return ValueTask.FromResult(
                httpResponseUtil.NoBody(
                    FieldKitAddItemResponse.Failure(
                        "That item instance already exists.")));
        }

        var item = new Item
        {
            Id = itemId,
            Template = templateId,
            Upd = itemHelper.GenerateUpdForItem(template!),
            SlotId = string.Empty
        };
        item.Upd.StackObjectsCount ??= 1;
        profile.Inventory.Items.Add(item);
        _preparedItemIds.Add(itemId);

        return ValueTask.FromResult(
            httpResponseUtil.NoBody(
                new FieldKitAddItemResponse(
                    true,
                    false,
                    "Item prepared for inventory placement.")));
    }

    public ValueTask<string> CancelPreparedItem(
        FieldKitCancelItemRequest request,
        MongoId sessionId)
    {
        MongoId itemId;
        try
        {
            itemId = new MongoId(request.ItemId);
        }
        catch
        {
            return ValueTask.FromResult(
                httpResponseUtil.NoBody(
                    FieldKitAddItemResponse.Failure(
                        "The item instance ID is invalid.")));
        }

        PmcData? profile = profileHelper.GetPmcProfile(sessionId);
        List<Item>? inventoryItems = profile?.Inventory?.Items;
        Item? item = inventoryItems?
            .FirstOrDefault(entry => entry.Id == itemId);
        if (item is not null &&
            item.ParentId is null &&
            _preparedItemIds.Remove(itemId))
            inventoryItems!.Remove(item);

        return ValueTask.FromResult(
            httpResponseUtil.NoBody(
                new FieldKitAddItemResponse(
                    true,
                    false,
                    "Prepared item cleared.")));
    }

    private bool TryGetTemplate(
        string templateIdText,
        out MongoId templateId,
        out TemplateItem? template,
        out string? error)
    {
        templateId = default;
        template = null;
        error = null;
        try
        {
            templateId = new MongoId(templateIdText);
        }
        catch
        {
            error = "The item template ID is invalid.";
            return false;
        }

        var result = itemHelper.GetItem(templateId);
        if (!result.Key || result.Value is null)
        {
            error = "That item template does not exist.";
            return false;
        }

        template = result.Value;
        return true;
    }

    private static MongoId CreateMongoId()
    {
        string value = Guid.NewGuid().ToString("N")[..24];
        return new MongoId(value);
    }
}

public record FieldKitAddItemRequest : IRequestData
{
    public string TemplateId { get; init; } = string.Empty;
    public int Amount { get; init; } = 1;
}

public record FieldKitPrepareItemRequest : IRequestData
{
    public string TemplateId { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
}

public record FieldKitCancelItemRequest : IRequestData
{
    public string ItemId { get; init; } = string.Empty;
}

public sealed record FieldKitAddItemResponse(
    bool Success,
    bool NoSpace,
    string Message)
{
    public static FieldKitAddItemResponse Failure(string message) =>
        new(false, false, message);
}
