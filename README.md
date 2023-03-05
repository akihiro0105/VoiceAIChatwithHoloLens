# VoiceAIChatwithHoloLens
- HoloLensの音声認識、合成機能とOpenAIのChat機能を利用して対話できるシステム

## プロジェクト設定
- Assets Storeからユニティちゃんモデルをインポートする
    - https://assetstore.unity.com/packages/3d/characters/unity-chan-model-18705
- ユニティちゃんのMaterialをUnlitのTextureかTransparentに変更する(HoloLensだと片目しか表示されないため)
- OpenAIのアカウントを作成 https://openai.com/api/
- API keysから API keyを生成してコピー
- `Assets\VoiceChat\Scripts\VoiceChatGPT.cs` のapikey設定箇所に貼り付け

## 利用Assets
- Mixed Reality Toolkit
    - https://github.com/microsoft/MixedRealityToolkit-Unity
- unity-chan!
    - https://assetstore.unity.com/packages/3d/characters/unity-chan-model-18705
- uLipSync
    - https://github.com/hecomi/uLipSync