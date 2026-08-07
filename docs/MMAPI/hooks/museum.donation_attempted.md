# Hook: museum.donation_attempted

Observe an attempted museum donation before the museum records it.

`museum.donation_attempted` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for registration and dispatch details.

## Contract

The event fires at the start of `donate_item_to_museum(item_id)`, before the item is registered, renown is queued, or completed-set results are calculated.

The callback receives `ctx.item_id`, the item identifier being donated.

This is intentionally an attempted-donation event. It does not report the eventual `DonationResult`, because the engine function has several return paths and the same item may already be registered. It is observation-only and cannot veto the donation.

## Usage

```gml
function record_museum_donation(_ctx) {
    // Inspect _ctx.item_id or update a mod-specific statistic.
}

mmapi_on("museum.donation_attempted", record_museum_donation);
```

With no handlers, the original registration and result calculation run unchanged.
