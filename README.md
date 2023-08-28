# Modular Avatar Expression Generator
衣装の各パーツごとにオンオフできるメニューを生成するためのツール。  
[Modular Avatar](https://modular-avatar.nadena.dev/)（以下MA）で導入される衣装に対して使用することを想定しています。  

![image](https://github.com/Gomorroth/MAExpressionGenerator/assets/70315656/1df2d86a-5d62-47bc-b76f-ffcd552cfe00)

## 導入
VCCから導入してください。リポジトリは[ここ](https://gomorroth.github.io/vpm-repos/)から追加できます。

## 使用方法
基本的に、導入後に自動生成される`Assets`フォルダ下の`MAExpressionGenerator`フォルダ内にある各種プレハブを配置します。

### Expression Generator
衣服のオンオフメニューを生成するための物です。

服のプレハブ内に`ExpressionGenerator.prefab`を配置し、`Run`を押してメニューを生成します。

![image](https://github.com/Gomorroth/MAExpressionGenerator/assets/70315656/beaa683b-d532-449d-a04b-ca513d162980)
![image](https://github.com/Gomorroth/MAExpressionGenerator/assets/70315656/24330529-3d63-4b05-b9d4-3d65ac70b8ec)  

- `Target`の欄に対象のオブジェクトが列挙されます。チェックを外すと対象からは外れます。
- `Run`ボタンを押した際のオブジェクトのオンオフの状態が初期状態となります。
  - 最初は非表示にしておきたい、というときはオブジェクトをオフにしておけば大丈夫です


### Expression Preset
衣装のプリセットを設定するための物です。

アバター内の適当な場所に`ExpressionPreset.prefab`を配置して設定をしてください。  
プリセットはプレイモード時・アバタービルド時に自動で生成されます。

![image](https://github.com/Gomorroth/MAExpressionGenerator/assets/70315656/b1da2f2c-7a45-4122-a5de-d134e0cff89a)

- `Sync`ボタン
  - ヒエラルキー上のオブジェクトの現在の状態をプリセットに反映します。
- `Apply`ボタン
  - プリセットをヒエラルキー上のオブジェクトに適応します。

### Expression Preset Manager
プリセットのメニュー生成先を変更するための物です。

適当な場所に配置して付属の`MA Menu Installer`を変更してください。
