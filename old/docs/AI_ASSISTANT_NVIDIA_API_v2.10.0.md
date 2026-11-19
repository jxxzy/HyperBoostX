# AI Assistant NVIDIA API v2.10.0

Release status: HyperBoostX v2.10.0 Owner Lab / Release Candidate

Status: PASS for source contract; live NVIDIA calls require an owner API key.

## Contract

- Provider: NVIDIA API via `https://integrate.api.nvidia.com/v1/chat/completions`.
- Registry: 10 NVIDIA model entries in `AiModelRegistry`.
- Default model: `nvidia/nemotron-3-nano-30b-a3b`.
- Fallback model: `nvidia/nvidia-nemotron-nano-9b-v2`.
- Secrets: API keys and bearer tokens are redacted by `AiSecretRedactor` and release evidence redaction.
- Output: strict JSON plan with `safe_actions`, `risk_level`, `requires_admin`, `restore_available`, and skipped unsafe actions.
- Apply rule: AI cannot auto apply. Suggestions require preview, Safety Guard, and user approval.

## Public Naming

The public UI calls this surface `NVIDIA API Provider`. The old internal class/key name remains only for compatibility with tests and stored routes.

