# AGENTS.md

## Android builds — versionamento (regra permanente)

Antes de CADA novo build de APK, incrementar a versão do projeto via Unity eval:

- `PlayerSettings.bundleVersion`: bump de minor (`1.1` → `1.2` → ...)
- `PlayerSettings.Android.bundleVersionCode`: +1 (2, 3, ...)
- Persistir com `AssetDatabase.SaveAssets()`
- Depois do build bem-sucedido, criar cópia versionada do APK: `Builds/Android/MVP Gen v<version>.apk` (manter o `MVP Gen.apk` original)

Comandos de build (nunca usar o `unity build` de topo): `unity command build --format json -- --confirm true` → poll `unity command build_status -- --build_id <id>`.
