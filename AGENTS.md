# AGENTS.md

## Time, aulas GTI e Graphify

- Conceito e papéis (diretor → QA, cyber, CI/CD, IA, smarthome, VR, interiores): [docs/TIME_AGENTICO.md](docs/TIME_AGENTICO.md)
- Alinhamento disciplinas × MVP: [docs/ALINHAMENTO_GTI.md](docs/ALINHAMENTO_GTI.md)
- Skills GTI: `.cursor/skills/gti-nlp`, `gti-ml-dl`, `gti-genai-prompt`, `gti-ai-driven-se`, `gti-casa-que-aprende`
- Unity: `.agents/skills/`
- Se `graphify-out/` existir, consultar Graphify antes de explorar `Assets/GenJutsu` com Grep

## Android builds — versionamento (regra permanente)

Antes de CADA novo build de APK, incrementar a versão do projeto via Unity eval:

- `PlayerSettings.bundleVersion`: bump de minor (`1.1` → `1.2` → ...)
- `PlayerSettings.Android.bundleVersionCode`: +1 (2, 3, ...)
- Persistir com `AssetDatabase.SaveAssets()`
- Depois do build bem-sucedido, criar cópia versionada do APK: `Builds/Android/MVP Gen v<version>.apk` (manter o `MVP Gen.apk` original)

Comandos de build (nunca usar o `unity build` de topo): `unity command build --format json -- --confirm true` → poll `unity command build_status -- --build_id <id>`.
