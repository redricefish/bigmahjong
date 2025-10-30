using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MahjongGameManager : MonoBehaviour
{
    // ゲームの状態を管理するEnum
    private enum GameState
    {
        HandOut,     // 1. 配牌モード (アニメーション中)
        Discard,     // 2. 捨て牌選択モード
        Replenish,   // 3. 牌の補充モード
        WinCheck,    // 4. 上がり判定モード
        Result,      // 5. 結果表示モード
        NextTurn     // 6. 次のターン準備 (タッチ待ち)
    }

    private GameState currentState = GameState.HandOut;

    // --- UI関連 (Inspectorで設定) ---
    public Button discardButton;
    public Button winButton;
    public TextMeshProUGUI resultText;

    // --- 牌のデータと設定 ---
    public float tileSpacing = 1.3f;      // 牌と牌の間隔 (重なり防止のため調整)
    public float displayDelay = 0.5f;     // 段階表示の時間差
    public float selectionOffset = 0.7f;  // 選択時に牌が上に移動する量
    public float tileScale = 0.5f;        // 牌の表示スケール

    private List<Sprite> allMahjongTiles;
    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> selectedTiles = new List<GameObject>(); // 複数選択用リスト

    void Start()
    {
        // 初期設定とUIの非表示
        LoadMahjongTiles();
        SetUIActive(false);
        resultText.gameObject.SetActive(false);

        // ボタンにイベントリスナーを追加
        discardButton.onClick.AddListener(OnDiscardButtonClick);
        winButton.onClick.AddListener(OnWinButtonClick);

        // ゲーム開始
        StartCoroutine(StartGame());
    }

    void Update()
    {
        if (currentState == GameState.Discard)
        {
            HandleTileSelection();
        }

        // 画面タッチでリセット
        if (currentState == GameState.NextTurn && Input.GetMouseButtonDown(0))
        {
            ResetAndRestart();
        }
    }

    private void SetUIActive(bool active)
    {
        discardButton.gameObject.SetActive(active);
        winButton.gameObject.SetActive(active);
    }

    // --------------------------------------------------------------------------------
    // 1. 配牌モード
    // --------------------------------------------------------------------------------
    private IEnumerator StartGame()
    {
        currentState = GameState.HandOut;
        resultText.gameObject.SetActive(false);

        List<Sprite> shuffledTiles = new List<Sprite>(allMahjongTiles);
        shuffledTiles.Shuffle();

        int totalTiles = 14;
        float startX = -(totalTiles - 1) * tileSpacing / 2f;
        int displayedCount = 0;

        // 4枚ずつ3回表示
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (displayedCount >= totalTiles) break;

                Sprite tileSprite = shuffledTiles[displayedCount];
                GameObject tileObject = CreateTileGameObject(tileSprite,
                    new Vector3(startX + displayedCount * tileSpacing, 0, 0));

                playerHand.Add(tileObject);
                displayedCount++;
            }
            yield return new WaitForSeconds(displayDelay);
        }

        // 最後に2枚表示
        for (int i = 0; i < 2; i++)
        {
            if (displayedCount >= totalTiles) break;

            Sprite tileSprite = shuffledTiles[displayedCount];
            GameObject tileObject = CreateTileGameObject(tileSprite,
                new Vector3(startX + displayedCount * tileSpacing, 0, 0));

            playerHand.Add(tileObject);
            displayedCount++;
        }

        // 配牌完了後、捨て牌選択モードへ
        yield return new WaitForSeconds(displayDelay);
        currentState = GameState.Discard;
        SetUIActive(true);
    }

    // --------------------------------------------------------------------------------
    // 2. 捨て牌選択モード
    // --------------------------------------------------------------------------------
    private void HandleTileSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedTile = hit.collider.gameObject;

                if (playerHand.Contains(clickedTile))
                {
                    if (selectedTiles.Contains(clickedTile))
                    {
                        // 選択解除: Y座標を元に戻す
                        clickedTile.transform.position -= new Vector3(0, selectionOffset, 0);
                        selectedTiles.Remove(clickedTile);
                    }
                    else
                    {
                        // 新規選択: Y座標を上げる
                        clickedTile.transform.position += new Vector3(0, selectionOffset, 0);
                        selectedTiles.Add(clickedTile);
                    }
                }
            }
        }
    }

    private void OnDiscardButtonClick()
    {
        if (selectedTiles.Count > 0)
        {
            // 全ての選択牌を処理
            foreach (GameObject tileToDiscard in selectedTiles)
            {
                // 捨て牌として画面上部に移動（Destroyするまでの仮表示）
                tileToDiscard.transform.position = new Vector3(tileToDiscard.transform.position.x, 3, 0);

                // 手牌リストから削除
                playerHand.Remove(tileToDiscard);

                // 注: 実際はここで捨て牌リストに追加するロジックが必要
                Destroy(tileToDiscard, 1.0f); // 1秒後に捨て牌を削除
            }

            selectedTiles.Clear();

            // UIを非表示にし、次の処理へ
            SetUIActive(false);
            StartCoroutine(RearrangeAndReplenish());
        }
    }

    private IEnumerator RearrangeAndReplenish()
    {
        // 牌を詰めて並べ直す
        yield return StartCoroutine(RearrangeHand());

        // 補充モードに移行
        yield return StartCoroutine(ReplenishTile());

        // ソートと最終整列
        yield return StartCoroutine(SortHand());

        // 捨て牌選択モードに戻る
        currentState = GameState.Discard;
        SetUIActive(true);
    }

    private IEnumerator RearrangeHand()
    {
        // 0.1秒待ってから並べ直す (捨て牌のアニメーションを見るため)
        yield return new WaitForSeconds(0.1f);

        float startX = -(playerHand.Count - 1) * tileSpacing / 2f;
        for (int i = 0; i < playerHand.Count; i++)
        {
            // アニメーションを滑らかにするなら Lerp を使う
            playerHand[i].transform.position = new Vector3(startX + i * tileSpacing, 0, 0);
        }
    }

    // --------------------------------------------------------------------------------
    // 3. 牌の補充モード
    // --------------------------------------------------------------------------------
    private IEnumerator ReplenishTile()
    {
        currentState = GameState.Replenish;
        int tilesNeeded = 14 - playerHand.Count;

        for (int i = 0; i < tilesNeeded; i++)
        {
            // 牌の山からランダムに1枚引く
            if (allMahjongTiles.Count > 0)
            {
                Sprite newTileSprite = allMahjongTiles[Random.Range(0, allMahjongTiles.Count)];

                // 最後に引いた牌を右端に表示
                Vector3 position = new Vector3((playerHand.Count) * tileSpacing + 0.5f, 0, 0);

                GameObject newTileObject = CreateTileGameObject(newTileSprite, position);
                playerHand.Add(newTileObject);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator SortHand()
    {
        // 現在は座標の再配置のみ（ソートロジックは実装外）
        float startX = -(playerHand.Count - 1) * tileSpacing / 2f;
        for (int i = 0; i < playerHand.Count; i++)
        {
            playerHand[i].transform.position = new Vector3(startX + i * tileSpacing, 0, 0);
        }

        yield return null;
    }

    // --------------------------------------------------------------------------------
    // 4 & 5. 上がり判定モードと結果表示
    // --------------------------------------------------------------------------------
    private void OnWinButtonClick()
    {
        if (selectedTiles.Count == 0 && currentState == GameState.Discard)
        {
            currentState = GameState.WinCheck;
            SetUIActive(false);
            StartCoroutine(CheckWinCondition());
        }
    }

    private IEnumerator CheckWinCondition()
    {
        yield return new WaitForSeconds(0.5f);

        // 仮の判定（ランダムで表示）
        resultText.gameObject.SetActive(true);
        if (Random.value > 0.5f)
        {
            resultText.text = "上がり！";
        }
        else
        {
            resultText.text = "上がってない";
        }

        currentState = GameState.NextTurn;
    }

    // --------------------------------------------------------------------------------
    // 6. 次のターン
    // --------------------------------------------------------------------------------
    private void ResetAndRestart()
    {
        // 既存の牌をすべて削除
        foreach (GameObject tile in playerHand)
        {
            Destroy(tile);
        }
        playerHand.Clear();
        selectedTiles.Clear(); // 選択リストもクリア

        // ゲームを最初から再開
        StartCoroutine(StartGame());
    }

    // --------------------------------------------------------------------------------
    // ユーティリティ関数
    // --------------------------------------------------------------------------------
    private GameObject CreateTileGameObject(Sprite sprite, Vector3 position)
    {
        GameObject tileObject = new GameObject(sprite.name);
        tileObject.transform.position = position;
        tileObject.transform.localScale = new Vector3(tileScale, tileScale, 1);

        SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        // 【重要】牌の選択（タッチ）のためにコライダーを追加
        BoxCollider2D collider = tileObject.AddComponent<BoxCollider2D>();
        // Colliderのサイズをスプライトのサイズに自動で合わせる
        collider.size = renderer.bounds.size;

        return tileObject;
    }

    private void LoadMahjongTiles()
    {
        // ... (牌のファイル名に基づく読み込みロジックは省略) ...
        // (以前のコードの LoadMahjongTiles メソッドの内容を使用してください)
        // 省略せず実装する場合は、以下の様にファイル名を指定して読み込む必要があります。

        allMahjongTiles = new List<Sprite>();

        for (int i = 1; i <= 9; i++)
        {
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_m_{i}"));
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_p_{i}"));
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_s_{i}"));
        }
        // 字牌
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_t")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_n"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_s")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_p"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_w")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_g"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_r"));

        allMahjongTiles.RemoveAll(item => item == null);

        if (allMahjongTiles.Count == 0)
        {
            Debug.LogError("麻雀牌のスプライトが見つかりません。Resources/MahjongTiles フォルダに配置してください。");
        }
    }
    private int[] tmp_wk = new int[14];

    private void CopyHandToTmp()
    {
        for (int i = 0; i < playerHand.Count && i < tmp_wk.Length; i++)
        {
            GameObject tileObj = playerHand[i];
            SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
            string name = sr.sprite.name; // ex. "mjpai_m_1"

            int value = 0;

            if (name.StartsWith("mjpai_m_"))        // 萬子
                value = 0x00 | int.Parse(name.Substring(8));
            else if (name.StartsWith("mjpai_p_"))   // 筒子
                value = 0x10 | int.Parse(name.Substring(8));
            else if (name.StartsWith("mjpai_s_"))   // 索子
                value = 0x20 | int.Parse(name.Substring(8));
            else if (name.StartsWith("mjpai_j_"))   // 字牌
            {
                string c = name.Substring(8, 1);
                switch (c)
                {
                    case "t": value = 0x30 | 1; break; // 東
                    case "n": value = 0x30 | 2; break; // 南
                    case "s": value = 0x30 | 3; break; // 西
                    case "p": value = 0x30 | 4; break; // 北
                    case "w": value = 0x30 | 5; break; // 白
                    case "g": value = 0x30 | 6; break; // 發
                    case "r": value = 0x30 | 7; break; // 中
                }
            }

            tmp_wk[i] = value;
        }
    }
    private void CopyTmpToHand()
    {
        // 既存の GameObject を削除
        foreach (GameObject tile in playerHand)
            Destroy(tile);
        playerHand.Clear();

        float startX = -(tmp_wk.Length - 1) * tileSpacing / 2f;

        for (int i = 0; i < tmp_wk.Length; i++)
        {
            int code = tmp_wk[i];
            if (code == 0) continue;

            string spriteName = "";

            int suit = code & 0xF0; // 上位4ビット
            int num = code & 0x0F; // 下位4ビット

            switch (suit)
            {
                case 0x00: spriteName = $"mjpai_m_{num}"; break; // 萬
                case 0x10: spriteName = $"mjpai_p_{num}"; break; // 筒
                case 0x20: spriteName = $"mjpai_s_{num}"; break; // 索
                case 0x30: // 字牌
                    switch (num)
                    {
                        case 1: spriteName = "mjpai_j_t"; break;
                        case 2: spriteName = "mjpai_j_n"; break;
                        case 3: spriteName = "mjpai_j_s"; break;
                        case 4: spriteName = "mjpai_j_p"; break;
                        case 5: spriteName = "mjpai_j_w"; break;
                        case 6: spriteName = "mjpai_j_g"; break;
                        case 7: spriteName = "mjpai_j_r"; break;
                    }
                    break;
            }

            Sprite sp = Resources.Load<Sprite>($"MahjongTiles/{spriteName}");
            Vector3 pos = new Vector3(startX + i * tileSpacing, 0, 0);
            GameObject obj = CreateTileGameObject(sp, pos);
            playerHand.Add(obj);
        }
    }

}

// Listの拡張メソッド
public static class ListExtension
{
    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
