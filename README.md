# VRMHelper

詳細は [こちらの記事](https://qiita.com/but80/items/3cc28cd874764daf3e58) をお読みください。

## 導入手順

[UniVRM](https://github.com/vrm-c/UniVRM) に依存しますので、まずこれがプロジェクトにインポート済みであることを前提とします。

[Releases](https://github.com/but80/VRMHelper/releases) ページから `VRMHelper-v*.unitypackage` をダウンロードし、プロジェクトにインポートしてください。

Pull Request を送っていただく場合は本リポジトリをforkし、以下のように既存のプロジェクトにサブモジュールとして取り込む形で変更を加えられます。

```bash
cd Assets
git submodule add https://github.com/(your-name)/VRMHelper.git
```

## 使用方法

パッケージをインポートすると、以下のメニュー項目が追加されます。`Open Setup Window` で設定ウィンドウを表示してください。

![](https://qiita-image-store.s3.ap-northeast-1.amazonaws.com/0/34010/752673f1-f88e-8048-0798-2d160fb1607c.png)

プロジェクトアセット中のVRMモデルプレハブを選択状態にし、各項目を設定して「適用」をクリックしてください。

![](https://qiita-image-store.s3.ap-northeast-1.amazonaws.com/0/34010/b5cf4e4b-9329-10bc-3ce4-8222c27fc398.png)

## 注意点

- 「長い前髪が顔に埋まる」対策により追加されるコライダーは、制作者の手元のモデルで位置とサイズを調整した球を基準に生成されるため、適用先のモデルによっては顔からはみ出たり大きさが足りなかったりする可能性があります。
  コライダーは `J_Bip_C_Head` にアタッチされている `VRM Spring Bone Collider Group` コンポーネントに追加されていますので、各自調整してください。
