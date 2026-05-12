# Logging

This directory contains the structured logging system used across NGO. The supported entry point is `ContextualLogger` together with the `Context` value type. The older `if (NetworkLog.CurrentLogLevel <= LogLevel.X) NetworkLog.LogX(...)` pattern is being phased out — prefer the `Context`-based API for new code.

## Two dimensions

NGO logging has two **independent** dimensions, set explicitly at every call site. Severity controls how Unity *displays* the line; verbosity controls whether NGO *emits* it at all.

| | Severity (`UnityEngine.LogType`) | Verbosity (`NetworkLog.LogLevel`) |
|---|---|---|
| Set by | `log.Info` → `Log` · `log.Warning` → `Warning` · `log.Error` → `Error` · `log.Exception` → `Exception` (bypasses verbosity) | `Context.Level`: `Developer` < `Normal` < `Error` < `None` |
| Filtered against | — (always reaches Unity if emitted) | `NetworkManager.LogLevel` threshold (`if (m_ManagerContext.LogLevel > context.Level) return;` in `ContextualLogger.Log`) |
| Scope | Per-message | Per-message tag, gated per-`NetworkManager` |

Server-routed variants (`InfoServer` / `WarningServer` / `ErrorServer`) additionally forward the message to the server/session-owner via `LogContextNetworkManager.TrySendMessage`.

## Creating a logger

Before constructing a new `ContextualLogger`, check what's already available:

1. **Does the surrounding type already have a `Log` field?** (e.g. `NetworkManager.Log`, `NetworkPrefabsList.Log`.) Use it.
2. **Do you have a `NetworkManager` reference but no obvious Unity object to attribute to?** Use `networkManager.Log` directly instead of constructing a new logger.
3. **Otherwise**, construct one with the explicit `(Object, NetworkManager)` constructor — always pass both, and cache the result as a `private` field rather than constructing per call.

### What the constructor arguments control

The `NetworkManager` argument decides which `LogLevel` threshold gates the logger (`LogContextNetworkManager.LogLevel`), which connection `*Server` variants route over, and which `LocalClientId` they tag as the sender. The `UnityEngine.Object` argument decides which object Unity's Console pings when the user clicks the log line — pass `this` whenever the surrounding type derives from `UnityEngine.Object`. For a one-off line about a different object, override per-message with `Context.AddObject(...)` rather than spinning up a second logger.

The two-argument constructor is the only form that gates the logger on the right `NetworkManager`. The single-argument and parameterless constructors fall back to tracking `NetworkManager.Singleton`, which silently misroutes logs in any setup with more than one manager (integration tests, host + client in one process, distributed authority).

### Bootstrap in `Awake`, rebind once the manager is known

For types that derive from `UnityEngine.Object` (`MonoBehaviour`, `NetworkBehaviour`, `ScriptableObject`), the relevant `NetworkManager` usually isn't available at `Awake` time — but you still want a usable logger as soon as the object exists.

1. **In `Awake`**, construct the logger with `new ContextualLogger(this)` so it has Console attribution immediately. This is the only sanctioned use of the singleton-tracking constructor — accept it as a transient state.
2. **As soon as the relevant `NetworkManager` is set** (`OnNetworkPreSpawn` for `NetworkBehaviour`, or wherever the manager reference lands for owned subsystems), **recreate** the logger as `new ContextualLogger(this, networkManager)`.

Do not skip the rebind — a logger that stays on the singleton fallback is the bug this guidance exists to prevent.

```csharp
internal class MyNetworkBehaviour : NetworkBehaviour
{
    private ContextualLogger m_Log;

    private void Awake() => m_Log = new ContextualLogger(this);

    public override void OnNetworkPreSpawn() => m_Log = new ContextualLogger(this, NetworkManager);
}
```

## Writing a log line

Construct a `Context` per call: `new Context(level, message)`, optionally enriched fluently with `AddTag(name)`, `AddInfo(key, value)`, or `AddObject(unityObject)`. The calling member is captured via `[CallerMemberName]` unless suppressed.

```csharp
log.Warning(new Context(LogLevel.Developer, "Serialized type not optimized for DA").AddTag(type.Name));
log.Info(new Context(LogLevel.Normal, "Connection approved"));
log.Error(new Context(LogLevel.Error, "Transport handshake failed"));
```

**Pair severity and verbosity sensibly.** Tag info-shaped messages with `Developer` or `Normal`, never `Error` — at `LogLevel.Error` only `Warning`/`Error` severities should appear. `Warning`/`Error` may carry any verbosity (a `Warning` tagged `Developer` is a noisy diagnostic warning gated behind chatty mode).

**Only use compile-time-constant strings in `Message`.** Interpolation runs *before* `Context` reaches the logger, so `$"…{value}…"` always allocates — even when the verbosity gate filters the line out. The only acceptable interpolation is one the compiler can fold to a `const` (literals + `nameof(...)`). Hand any runtime value to `Context` via `AddTag`/`AddInfo`/`AddObject`; the builder only runs when the gate passes. If `Context`/`LogBuilder` can't express what you need, **extend them** rather than reach for `$"…"` at the call site — that's how the API is meant to grow.

```csharp
// Bad — interpolation runs unconditionally, before the verbosity check
log.Info(new Context(LogLevel.Developer, $"Spawned {obj.name} with id {obj.NetworkObjectId}"));

// Good — literal + nameof folds at compile time; runtime values are added as structured info
log.Info(new Context(LogLevel.Developer, $"Spawned {nameof(NetworkObject)}").AddTag(obj.name).AddInfo(nameof(obj.NetworkObjectId), obj.NetworkObjectId));
```

Never wrap a `ContextualLogger` call in `if (NetworkLog.CurrentLogLevel <= …)` — `Context.Level` is the verbosity gate. All `ContextualLogger` methods are `[Conditional("UNITY_ASSERTIONS")]`, so calls (and the `Context` allocation) compile out of release builds.

## File map

- `LogLevel.cs` — public verbosity enum (`Developer` / `Normal` / `Error` / `None`).
- `LogContext.cs` — `Context` value type (per-message payload: level, message, tags, info, object override).
- `ContextualLogger.cs` — the logger; combines system-wide context (`LogContextNetworkManager`, `GenericContext`) with per-call `Context` and forwards to `Debug.unityLogger`. Also handles the verbosity gate.
- `LogContextNetworkManager.cs` — system-wide context tied to a `NetworkManager` (holds the threshold, handles server/session-owner forwarding).
- `GenericContext.cs` — pooled key/value + tag store used by both per-logger and per-message context.
- `LogBuilder.cs` — pooled string assembly for the final formatted line.
- `NetworkLog.cs` — legacy static façade. Still in use; new code should prefer `ContextualLogger` directly.
