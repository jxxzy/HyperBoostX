# HyperBoostX V3 Security Threat Model

**Status:** Design and source-grounded risk assessment; not a security audit or release approval.  
**Reviewed:** 2026-11-16  
**Scope:** The current Windows desktop application and the proposed boundary for any future privileged worker.

## 1. System boundary and verified baseline

HyperBoostX is a .NET 8 WPF application. Its manifest requests `asInvoker`; it does not currently install or run a privileged worker, Windows service, or authenticated IPC server. System actions are registered in-process by `Runtime` and dispatched through `ActionEngine` using fixed `ActionInfo.Id` values and typed `ISystemAction` implementations. The current action registry contains seven mutations, scoped to current-user settings, the active Windows power plan, current-user startup entries, or temporary-file quarantine.

For these actions, the engine validates the action and environment, previews the current and desired state, requires explicit approval, re-reads state under an exclusive journal lock, saves a durable recovery record before a write, reads the result back, and attempts a conflict-aware restore after failure. This is a safety workflow, not a privilege boundary: compromise of the desktop process can bypass in-process checks.

Recovery and application documents are protected with Windows DPAPI using `CurrentUser`. `SafePaths` rejects network and alternate-stream paths, reparse points, and paths escaping the managed root. These protections do not defend against malware running as the same Windows user or a hostile administrator.

The application uses a per-user `Local\\` mutex and activation event. The activation event sends only a signal, not a command payload; the application does not set an explicit security descriptor on these objects. No named-pipe or other privileged IPC endpoint is currently implemented. External Windows control-panel/tool handoffs are a fixed in-app allowlist and do not constitute an application-managed privileged operation. The application opens no arbitrary elevated command runner.

These statements describe the inspected source baseline only. They do not prove that the built artifact is signed, that all dependencies are safe, or that the runtime is free of vulnerabilities.

## 2. Security objectives

1. Keep the desktop process at the user's normal integrity level unless a specific, justified operation requires elevation.
2. Never turn natural-language AI output, plugin content, UI text, or imported files into commands or unbounded privileged requests.
3. Make every supported mutation finite, typed, allowlisted, validated again at the point of execution, consented to, recoverable, and verifiable.
4. Preserve security, anti-cheat, driver, thermal, cooling, and critical system controls. Do not offer actions that disable them.
5. Keep credentials and recovery material out of ordinary configuration exports and redact secrets and personal data from logs and reports.
6. Fail closed on unknown actions, incompatible systems, malformed input, denied access, stale state, and unavailable recovery evidence.
7. Do not claim a capability, compatibility, security property, or release trust state without runtime evidence.

## 3. Assets and trust boundaries

| Asset or boundary | Security concern |
|---|---|
| Windows configuration and user data | Unauthorized or partial mutation, data loss, or conflict with management policy |
| Recovery records and action history | Tampering or deletion could prevent safe restore; records may reveal user activity |
| API credentials and integration payloads | Disclosure to other users, logs, exports, or unapproved network recipients |
| Application binaries, packages, and update metadata | Replacement or rollback to untrusted code |
| UI-to-action path | Forged, stale, or unintended action request; in-process validation is not a security boundary |
| Future desktop-to-worker IPC | Spoofed client/server, confused deputy, replay, malformed data, or cross-user access |
| User profile and filesystem | Path traversal, reparse-point redirection, unsafe import/export, or same-user tampering |
| External services and integrations | Untrusted responses, endpoint changes, data disclosure, and network interruption |
| Diagnostic providers and hardware | Unsupported or privileged APIs can produce misleading or sensitive results |

The desktop application, operating system, current-user profile, local files, network services, and any future elevated worker are distinct trust boundaries. A signature, DPAPI, named-object name, action preview, or successful UI test does not independently establish trust across those boundaries.

## 4. Threats and current disposition

| Threat | Current controls and limitation | Required disposition |
|---|---|---|
| Unapproved or arbitrary elevated commands | No privileged worker or generic elevated command runner exists. Current action IDs are fixed and registered in-process. | Keep unknown IDs rejected. A future worker must expose only fixed typed handlers; never accept command lines, scripts, executable paths, or AI-generated instructions. |
| Compromised desktop process bypasses safety checks | The app runs `asInvoker`; safety checks, preview, approval, and action dispatch are in the same process. | Treat desktop compromise as able to exercise that user's current-user capabilities. The future worker must independently validate every request and must not trust UI-supplied risk, state, or consent claims. |
| Spoofed or unauthorized local activation | Current activation is a payload-free local event; no explicit DACL is configured. A same-user process may signal it. | Activation only restores the existing window; still test object squatting, access denial, and cross-user isolation. Do not reuse this mechanism for privileged commands. |
| IPC spoofing, replay, or confused deputy | No privileged IPC endpoint exists today. | Before implementation, define explicit local-only pipe ACLs, verify the connected caller's Windows identity and session, use bounded versioned messages, bind one-time requests to the authenticated caller, and reject replay and unknown fields/actions. Independently validate in the worker. |
| Stale or conflicting Windows state | Apply re-reads state under an exclusive journal lock; rollback refuses to overwrite a state that differs from both the captured original and requested state. | Preserve these checks in any worker. Test concurrent writers and conflict reporting in an isolated Windows environment. |
| Crash or power loss during mutation | A recovery intent is persisted before mutation and recovery is reconciled at startup. Existing recovery is not proof of durable privileged-worker recovery across process/service/reboot boundaries. | A worker must own a durable, access-controlled transaction journal and recover idempotently after worker crash, application crash, and reboot. Never blindly overwrite a third-party state. |
| Recovery-file or settings path attack | Managed paths reject network paths, alternate streams, reparse points, and root escapes. DPAPI `CurrentUser` protects stored bytes from other users without the DPAPI context. | Continue canonical path validation at every filesystem boundary. Do not treat DPAPI as protection against same-user malware or an administrator. Test import/export and recovery paths under hostile inputs. |
| Malicious or unsafe plugin | A plugin manager route does not establish a safe plugin execution sandbox. Job Objects alone are not a complete security boundary. | Do not load plugins or grant plugin permissions until the execution model, identity, filesystem/network access, process limits, update trust, and escape tests are independently validated. |
| Integration data exfiltration or credential leakage | User consent and encrypted local storage are requirements; full secret-flow and recipient validation are not established by this document. | Require per-destination consent, HTTPS and endpoint validation, explicit payload preview, secret redaction, bounded timeouts, and credential exclusion from export/logs. Do not send data by default. |
| Malicious update or dependency | Current installer is unsigned; release provenance, complete SBOM/license review, and signed update verification are not established. | Pin and review dependencies, generate and verify SBOM/provenance, enforce signed update metadata and rollback policy, and keep public release disabled without owner authorization. |
| Denial of service or resource exhaustion | Preview count and several input/data limits are bounded, but not every provider or integration has a complete resource budget. | Bound IPC frames, concurrent operations, file sizes, network responses, retries, and execution time. Surface failure; do not retry indefinitely. |
| Sensitive diagnostics in reports or logs | Some reports and application errors are redacted; a complete data classification and retention review is outstanding. | Minimize collected fields, redact before persistence/export, document retention, and require consent before sharing diagnostics. |

## 5. Required privileged-worker design before implementation

The following is a security contract, not an assertion that a worker exists:

1. **Least privilege and lifetime:** keep the WPF app `asInvoker`. If elevation is required, use a separately built and trusted worker only for the specific approved operation. Prefer an on-demand, bounded lifetime; do not add a permanently privileged service without a reviewed necessity and owner approval.
2. **Authenticated local transport:** use a local-only IPC endpoint with an explicit restrictive Windows security descriptor. Reject remote clients. Authenticate the connected client using its actual Windows token/SID and logon session; do not authenticate from a caller-supplied username, PID, path, or UI state. Define and test how UAC cancellation and standard-user operation behave.
3. **Versioned typed protocol:** accept a small, size-bounded, versioned schema containing a stable `ActionId` and typed parameters only. Reject unknown actions, unknown versions, duplicate/conflicting fields, malformed values, out-of-range values, excess payloads, and unsupported capabilities. Never execute arbitrary text or a supplied path as a command.
4. **Independent authorization:** the worker owns the allowlist and revalidates OS version, capability, policy, protected target, parameter types, and current state. Only supported, reversible operations may be registered. Critical/security/anti-cheat/driver/cooling/thermal controls are not eligible.
5. **Preview and consent:** compute the before/after diff in the trusted handler. Bind a short-lived, single-use confirmation to the authenticated caller, action, typed target, expected-state fingerprint, and transaction ID. Show risk and restart impact before explicit user approval. UAC denial or user cancellation performs no mutation.
6. **Durable transaction:** serialize mutations; persist the exact typed before-state and recovery intent in an access-controlled durable store before writing. Re-read state immediately before mutation. After writing, verify by reading the actual OS state. On failure, restore only if the observed state is still the expected state; otherwise preserve it and report a recovery conflict.
7. **Safe cancellation and recovery:** cancellation is honored before mutation and at documented safe points. After mutation begins, cancellation must either complete a verified rollback or leave a durable recovery-required record. Startup/reboot recovery is bounded, idempotent, and never silently discards records or overwrites conflicting state.
8. **Auditable errors:** return stable error codes, operation ID, actual stage, cancellation/denial state, and non-secret evidence. Do not return success-shaped fallbacks or include credentials, full secrets, or unredacted command-line data.
9. **Trusted deployment:** the worker and update path must have an independently verified publisher/signature and an explicit version-compatibility policy. An unsigned local build may be tested only in a disposable environment and must not be presented as trusted production code.

## 6. Required isolated verification

No privileged mutation, UAC, access-denial, crash/reboot recovery, or installer lifecycle test may run against a user's everyday Windows installation. Run these only in disposable Windows VMs/snapshots with synthetic data. Before any Stable claim, record actual evidence for:

- allowed and denied action IDs; exact parameter validation; protected controls; capability mismatch;
- standard-user invocation, UAC approval, UAC denial, cancellation, and worker exit;
- caller SID/session mismatch, cross-user access denial, remote-client denial, endpoint squatting, malformed/oversized frames, replay, stale tokens, and worker restart;
- concurrent operations, external state change, partial write, disk-full, database/journal failure, process termination at each mutation phase, and reboot recovery;
- exact before/after verification, successful rollback, conflict-safe rollback refusal, and preserved recovery evidence;
- signed package identity, update trust/rollback rejection, dependency/SBOM evidence, and install/repair/uninstall behavior.

The ordinary production Test Center must remain non-destructive. These integration tests require a separately controlled disposable environment and must never be represented by route-navigation or mock-provider tests.

## 7. Explicit non-goals and release blockers

- This design does not authorize an elevated worker, Windows service, plugin runtime, or arbitrary optimization command.
- It does not claim that every current action is safe on every Windows/OEM configuration.
- It does not make HyperBoostX an antivirus, anti-cheat controller, hardware controller, or game-overlay/injection tool.
- It does not establish code signing, supply-chain provenance, full dependency/license compliance, successful privileged mutation, or Stable readiness.
- The worker remains unsupported until the protocol is implemented and independently tested in a disposable Windows environment. Current builds remain unsigned development artifacts.
