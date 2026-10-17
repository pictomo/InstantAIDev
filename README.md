# STARPAWS — Neon Orbit

宇宙を走る小さなネコ「Mochi」の、3Dエンドレスランゲームです。Unity 2022.3 LTS / Built-in Render Pipeline 対応。外部モデル・テクスチャ・音源は使用していません。

## 遊び方

1. Unityで `Assets/Scenes/StarPaws.unity` を開いて **Play**。
2. **スペース** または「さあ、出発！」でスタート。ネコは自動で前に走ります。
3. **← / →** で左右移動、**スペース** でジャンプ。A / D でも移動できます。
4. 足場は踏むと発光し、約1秒後に崩れます。隙間の少し手前でジャンプしましょう。
5. 星1つにつき100点。進んだ距離にも加点され、星を集めるほど7色の軌跡が長くなります。
6. 落ちたら **スペース** / **R** / 「もう一度」で即リトライ。

**Esc**: 一時停止・再開 / **M** または「音 ♪」: サウンド切替 / **R**: いつでもリトライ。

最高スコアは端末内の PlayerPrefs に保存します。ゲームからフォーカスが外れた場合は一時停止します。

## 実装

- `StarPawsGame.cs` — 状態管理、連続左右移動、ジャンプ、コース生成、スコア、リトライ。
- `CatAvatar.cs` — 球体と自作メッシュのネコ、走行アニメーション、7本の虹リボン。
- `StarPlatform.cs` — 接地判定、星、発光、ひび割れ、崩壊。
- `CosmicArt.cs` / `CosmicBackdrop.cs` — プロシージャルな形状、星空、惑星、パーティクル。
- `StarPawsHUD.cs` — 解像度に追従するUI。日本語は利用可能なOSフォントで描画。
- `CosmicAudio.cs` — コードで合成するBGMと効果音。
- `Shaders/` — 星空、柔らかい光、軽量Bloom。

ゲームのオブジェクトはPlay時に生成されます。足場の形状メッシュを共有し、後方のコースは破棄して先を生成します。0.13秒のコヨーテタイムと0.15秒のジャンプ先行入力を用意しています。左右移動はレーン固定ではありません。

`StarPaws > Run gameplay verification` で操作、着地、崩壊、収集、スコア、虹、一時停止、落下、リトライを検証し、`Library/StarPaws-verification.txt` に結果を出力します。検証中に記録した最高点は終了時に復元します。

## Mac版

`Builds/StarPaws.app` をダブルクリックすると、そのまま遊べます。`StarPaws > Build macOS app` で再ビルドできます。ビルド出力はGitの管理対象外です。

確認内容は `Documentation/Verification.md`、画面プレビューは `Documentation/StarPaws-preview.png` にあります。
