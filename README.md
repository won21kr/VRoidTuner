# VRoidTuner (Unofficial)

以下の環境にて動作確認を行っております。

- Windows 10 64bit
- Unity 2019.2.17f1
- VRoid Studio 0.8.1
- UniVRM v0.55.0

詳細は [こちらの記事](https://qiita.com/but80/items/3cc28cd874764daf3e58) をお読みください。

## 導入手順

[UniVRM](https://github.com/vrm-c/UniVRM) に依存しますので、まずこれがプロジェクトにインポート済みであることを前提とします。

[Releases](https://github.com/but80/VRoidTuner/releases) ページから `VRoidTuner-v*.unitypackage` をダウンロードし、プロジェクトにインポートしてください。

Pull Request を送っていただく場合は本リポジトリをforkし、以下のように既存のプロジェクトにサブモジュールとして取り込む形で変更を加えられます。

```bash
cd Assets
git submodule add https://github.com/(your-name)/VRoidTuner.git
```

## 使用方法

パッケージをインポートすると、以下のメニュー項目が追加されます。`Open Setup Window` で設定ウィンドウを表示してください。

![](https://qiita-image-store.s3.ap-northeast-1.amazonaws.com/0/34010/ef61e092-6a9d-d0a4-886e-d8d3f4780775.png)

プロジェクトアセット中のVRMモデルプレハブを選択状態にし、各項目を設定して「適用」をクリックしてください。

![](https://qiita-image-store.s3.ap-northeast-1.amazonaws.com/0/34010/0c9b469e-603b-3892-8afa-585887d30605.png)

[リリースページ](https://github.com/but80/VRoidTuner/releases) にて解説している機能もあります。ご確認ください。

## 注意点

- 「長い前髪が顔に埋まる」対策により追加されるコライダーは、制作者の手元のモデルで位置とサイズを調整した球を基準に生成されるため、適用先のモデルによっては顔からはみ出たり大きさが足りなかったりする可能性があります。
  コライダーは `J_Bip_C_Head` にアタッチされている `VRM Spring Bone Collider Group` コンポーネントに追加されていますので、各自調整してください。
- 本ツールは非公式なものであり、VRoid公式とは無関係です。質問は本リポジトリに [Issueを上げていただく](https://github.com/but80/VRMHelper/issues/new) か、[Twitter](https://twitter.com/bucchigiri) までお願いします。
