using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using MahjongLogic;

public class MahjongDisplayManager : MonoBehaviour
{
    // ゲームの状態を管理するEnum
    private enum GameState
    {
        HandOut,     // 1. 配牌モード (アニメーション中)
        Discard,     // 2. 捨て牌選択モード
        Replenish,   // 3. 牌の補充モード
        WinCheck,    // 4. 上がり判定モード
        Result,      // 5. 結果表示モード
        NextTurn,    // 6. 次のターン準備 (タッチ待ち)
        GameOver     // 7. ゲームオーバー（流局）状態を追加
    }

    private GameState currentState = GameState.HandOut;

    // --- UI関連 (Inspectorで設定) ---
    public Button discardButton;
    public Button winButton;
    public Button sortButton;
    public TextMeshProUGUI resultText;

    // --- 牌のデータと設定 ---
    public float tileSpacing = 1.3f;
    public float displayDelay = 0.5f;
    public float selectionOffset = 0.7f;
    public float tileScale = 0.5f;

    // allMahjongTiles は34種類の「マスターリスト」として使用
    private List<Sprite> allMahjongTiles;
    // gameDeck は136枚の「実際の山」として使用
    private List<Sprite> gameDeck = new List<Sprite>();
    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> selectedTiles = new List<GameObject>();
    private MahjongScoring mahjongScoring;

    void Start()
    {
        // 初期設定とUIの非表示
        LoadMahjongTiles(); // 34種のマスターをロード
        BuildDeck();        // 136枚の山を構築
        SetUIActive(false);
        resultText.gameObject.SetActive(false);

        // ボタンにイベントリスナーを追加
        discardButton.onClick.AddListener(OnDiscardButtonClick);
        winButton.onClick.AddListener(OnWinButtonClick);
        sortButton.onClick.AddListener(OnSortButtonClick);

        mahjongScoring = new MahjongScoring();
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
        // GameOver 状態でもリセットできるように変更
        if ((currentState == GameState.NextTurn || currentState == GameState.GameOver) && Input.GetMouseButtonDown(0))
        {
            ResetAndRestart();
        }
    }

    private void SetUIActive(bool active)
    {
        discardButton.gameObject.SetActive(active);
        winButton.gameObject.SetActive(active);
        sortButton.gameObject.SetActive(active);
    }

    // --------------------------------------------------------------------------------
    // 1. 配牌モード
    // --------------------------------------------------------------------------------
    private IEnumerator StartGame()
    {
        currentState = GameState.HandOut;
        resultText.gameObject.SetActive(false);

        // ★変更: allMahjongTiles ではなく gameDeck から引く
        // (BuildDeck() でシャッフル済み)

        int totalTiles = 14;
        float startX = -(totalTiles - 1) * tileSpacing / 2f;
        int displayedCount = 0;

        // 4枚ずつ3回表示
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (displayedCount >= totalTiles) break;

                // ★変更: DrawTile() を使用
                Sprite tileSprite = DrawTile();
                if (tileSprite == null) yield break; // 山が尽きた場合

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

            // ★変更: DrawTile() を使用
            Sprite tileSprite = DrawTile();
            if (tileSprite == null) yield break; // 山が尽きた場合

            GameObject tileObject = CreateTileGameObject(tileSprite,
                new Vector3(startX + displayedCount * tileSpacing, 0, 0));

            playerHand.Add(tileObject);
            displayedCount++;
        }

        // 配牌完了後、ソートして捨て牌選択モードへ
        yield return new WaitForSeconds(displayDelay);
        SortAndRedisplayHand();
        currentState = GameState.Discard;
        SetUIActive(true);
    }

    // --------------------------------------------------------------------------------
    // 2. 捨て牌選択モード
    // --------------------------------------------------------------------------------
    private void HandleTileSelection()
    {
        // ... (変更なし) ...
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
                        clickedTile.transform.position -= new Vector3(0, selectionOffset, 0);
                        selectedTiles.Remove(clickedTile);
                    }
                    else
                    {
                        clickedTile.transform.position += new Vector3(0, selectionOffset, 0);
                        selectedTiles.Add(clickedTile);
                    }
                }
            }
        }
    }

    private void OnDiscardButtonClick()
    {
        // ... (変更なし) ...
        if (selectedTiles.Count > 0)
        {
            foreach (GameObject tileToDiscard in selectedTiles)
            {
                tileToDiscard.transform.position = new Vector3(tileToDiscard.transform.position.x, 3, 0);
                playerHand.Remove(tileToDiscard);
                Destroy(tileToDiscard, 1.0f);
            }
            selectedTiles.Clear();
            SetUIActive(false);
            StartCoroutine(RearrangeAndReplenish());
        }
    }

    // --------------------------------------------------------------------------------
    // 5. ソートボタン処理
    // --------------------------------------------------------------------------------
    private void OnSortButtonClick()
    {
        // ... (変更なし) ...
        if (currentState != GameState.Discard) return;
        if (selectedTiles.Count > 0)
        {
            foreach (var tile in selectedTiles)
            {
                tile.transform.position -= new Vector3(0, selectionOffset, 0);
            }
            selectedTiles.Clear();
        }
        SortAndRedisplayHand();
    }

    // --------------------------------------------------------------------------------
    // 6. ソート実行ロジック
    // --------------------------------------------------------------------------------
    private void SortAndRedisplayHand()
    {
        // ... (変更なし) ...
        Array.Clear(tmp_wk, 0, tmp_wk.Length);
        CopyHandToTmp();
        Array.Sort(tmp_wk, 0, playerHand.Count);
        CopyTmpToHand();
    }


    private IEnumerator RearrangeAndReplenish()
    {
        // ... (変更なし) ...
        yield return StartCoroutine(RearrangeHand());
        yield return StartCoroutine(ReplenishTile());

        // ★注意: ReplenishTile でゲームオーバーになった場合、
        // この後の処理は実行されない（yield break するため）
        if (currentState == GameState.GameOver) yield break;

        yield return StartCoroutine(SortHand());
        currentState = GameState.Discard;
        SetUIActive(true);
    }

    private IEnumerator RearrangeHand()
    {
        // ... (変更なし) ...
        yield return new WaitForSeconds(0.1f);
        float startX = -(playerHand.Count - 1) * tileSpacing / 2f;
        for (int i = 0; i < playerHand.Count; i++)
        {
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
            // ★変更: allMahjongTiles からランダムに選ぶ代わりに、DrawTile() を使う
            Sprite newTileSprite = DrawTile();

            // ★追加: 山が尽きたらコルーチンを即時終了
            if (newTileSprite == null)
            {
                yield break;
            }

            // 最後に引いた牌を右端に表示 (仮の位置)
            float posX = (-(playerHand.Count - 1) * tileSpacing / 2f) + playerHand.Count * tileSpacing + 0.5f;

            GameObject newTileObject = CreateTileGameObject(newTileSprite, new Vector3(posX, 0, 0));
            playerHand.Add(newTileObject);

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator SortHand()
    {
        // ... (変更なし) ...
        SortAndRedisplayHand();
        yield return null;
    }

    // --------------------------------------------------------------------------------
    // 4 & 5. 上がり判定モードと結果表示
    // --------------------------------------------------------------------------------
    private void OnWinButtonClick()
    {
        // ... (変更なし) ...
        if (selectedTiles.Count == 0 && currentState == GameState.Discard)
        {
            currentState = GameState.WinCheck;
            SetUIActive(false);
            StartCoroutine(CheckWinCondition());
        }
    }

    private IEnumerator CheckWinCondition()
    {
        // ... (変更なし) ...
        yield return new WaitForSeconds(0.5f);
        CopyHandToTmp();
        int[] handToCheck = new int[14];
        Array.Copy(tmp_wk, handToCheck, 14);

        int winningTile = handToCheck[13];
        if (winningTile == 0 && handToCheck.Length > 0)
        {
            winningTile = handToCheck[12];
        }

        List<string> yakuNames = mahjongScoring.CheckWin(handToCheck, winningTile, 1, 1);

        resultText.gameObject.SetActive(true);
        if (yakuNames.Count > 0)
        {
            resultText.text = string.Join("\n", yakuNames);
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
        selectedTiles.Clear();

        // ★追加: 山を再構築
        BuildDeck();

        // ゲームを最初から再開
        StartCoroutine(StartGame());
    }

    // --------------------------------------------------------------------------------
    // ユーティリティ関数 (★新規関数追加)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// ★新規追加: 136枚のゲームデッキを構築し、シャッフルする
    /// </summary>
    private void BuildDeck()
    {
        gameDeck.Clear();
        if (allMahjongTiles == null || allMahjongTiles.Count == 0)
        {
            Debug.LogError("マスター牌リスト(allMahjongTiles)が空です。LoadMahjongTilesを先に実行してください。");
            return;
        }

        // 34種のマスターリストから、各種4枚ずつデッキに追加
        foreach (Sprite tileSprite in allMahjongTiles)
        {
            for (int i = 0; i < 4; i++)
            {
                gameDeck.Add(tileSprite);
            }
        }

        // デッキをシャッフル
        gameDeck.Shuffle();
    }

    /// <summary>
    /// ★新規追加: デッキ（山）から牌を1枚引く
    /// </summary>
    /// <returns>スプライト。山が尽きたら null</returns>
    private Sprite DrawTile()
    {
        // 136枚 - 14枚（王牌/リンシャン）= 122枚がツモれる
        // 山が14枚以下になったら「流局」とする
        if (gameDeck.Count <= 14)
        {
            StartCoroutine(ShowGameOver("（流局）"));
            return null;
        }

        // デッキの先頭から1枚引く
        Sprite tile = gameDeck[0];
        gameDeck.RemoveAt(0);
        return tile;
    }

    /// <summary>
    /// ★新規追加: ゲームオーバー（流局）処理
    /// </summary>
    private IEnumerator ShowGameOver(string message)
    {
        // 既にゲームオーバーなら何もしない
        if (currentState == GameState.GameOver) yield break;

        currentState = GameState.GameOver;
        resultText.text = "ゲームオーバー\n" + message;
        resultText.gameObject.SetActive(true);
        SetUIActive(false); // 全ボタンを非表示
    }


    private GameObject CreateTileGameObject(Sprite sprite, Vector3 position)
    {
        // ... (変更なし) ...
        GameObject tileObject = new GameObject(sprite.name);
        tileObject.transform.position = position;
        tileObject.transform.localScale = new Vector3(tileScale, tileScale, 1);

        SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        BoxCollider2D collider = tileObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);

        return tileObject;
    }

    private void LoadMahjongTiles()
    {
        // ... (変更なし) ...
        // この関数は「34種類のマスターリスト」を作成する役割のみになる
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

        if (allMahjongTiles.Count != 34) // 34種類あるか確認
        {
            Debug.LogError($"麻雀牌のスプライトの読み込みに失敗しました。34種類必要ですが、{allMahjongTiles.Count}種類しか読み込めませんでした。");
        }
    }

    private int[] tmp_wk = new int[14];

    private void CopyHandToTmp()
    {
        // ... (変更なし) ...
        Array.Clear(tmp_wk, 0, tmp_wk.Length);
        for (int i = 0; i < playerHand.Count && i < tmp_wk.Length; i++)
        {
            GameObject tileObj = playerHand[i];
            SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            string name = sr.sprite.name;
            int value = 0;

            if (name.StartsWith("mjpai_j_"))
            {
                string c = name.Substring(8, 1);
                switch (c)
                {
                    case "t": value = 0x30 | 1; break;
                    case "n": value = 0x30 | 2; break;
                    case "s": value = 0x30 | 3; break;
                    case "p": value = 0x30 | 4; break;
                    case "w": value = 0x30 | 5; break;
                    case "g": value = 0x30 | 6; break;
                    case "r": value = 0x30 | 7; break;
                }
            }
            else if (name.StartsWith("mjpai_m_"))
            {
                value = 0x00 | int.Parse(name.Substring(8, 1));
            }
            else if (name.StartsWith("mjpai_p_"))
            {
                value = 0x10 | int.Parse(name.Substring(8, 1));
            }
            else if (name.StartsWith("mjpai_s_"))
            {
                value = 0x20 | int.Parse(name.Substring(8, 1));
            }
            tmp_wk[i] = value;
        }
    }

    private void CopyTmpToHand()
    {
        // ... (変更なし) ...
        foreach (GameObject tile in playerHand)
            Destroy(tile);
        playerHand.Clear();

        int validTilesCount = 0;
        for (int i = 0; i < tmp_wk.Length; i++)
        {
            if (tmp_wk[i] != 0) validTilesCount++;
        }

        float startX = -(validTilesCount - 1) * tileSpacing / 2f;
        int displayIndex = 0;

        for (int i = 0; i < tmp_wk.Length; i++)
        {
            int code = tmp_wk[i];
            if (code == 0) continue;

            string spriteName = "";
            int suit = code & 0xF0;
            int num = code & 0x0F;

            switch (suit)
            {
                case 0x00: spriteName = $"mjpai_m_{num}"; break;
                case 0x10: spriteName = $"mjpai_p_{num}"; break;
                case 0x20: spriteName = $"mjpai_s_{num}"; break;
                case 0x30:
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
            if (sp == null)
            {
                Debug.LogWarning($"Sprite not found: MahjongTiles/{spriteName}");
                continue;
            }

            Vector3 pos = new Vector3(startX + displayIndex * tileSpacing, 0, 0);
            GameObject obj = CreateTileGameObject(sp, pos);
            playerHand.Add(obj);
            displayIndex++;
        }
    }

}

// Listの拡張メソッド
public static class ListExtension
{
    // ... (変更なし) ...
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