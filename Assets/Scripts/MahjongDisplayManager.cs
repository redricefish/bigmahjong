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
        HandOut,          // 1. 配牌
        DrawCandidates,   // 2. 入れ替え候補の牌を山から引く
        SelectCandidates, // 3. 候補の牌（5枚）を選択中
        SelectHandTiles,  // 4. 手牌から捨てる牌を選択中
        ProcessingSwap,   // 5. 牌の入れ替えと破棄を実行中
        WinCheck,         // 6. 上がり判定
        Result,           // 7. 結果表示
        NextTurn,         // 8. 次のターン準備 (タッチ待ち)
        GameOver          // 9. ゲームオーバー（流局）
    }

    private GameState currentState = GameState.HandOut;

    // --- UI関連 (Inspectorで設定) ---
    public Button discardButton; // 1つのボタンを「決定」「交換」など多目的に使います
    public Button winButton;
    public Button sortButton;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI discardButtonText; // 捨て牌ボタンのテキスト

    // --- 牌のデータと設定 ---
    public float tileSpacing = 1.3f;
    public float displayDelay = 0.5f;
    public float selectionOffset = 0.7f;
    public float tileScale = 0.5f;
    private Vector3 candidateTilePos = new Vector3(0, 2.5f, 0); // 候補牌のY座標

    // --- 牌リスト ---
    private List<Sprite> allMahjongTiles; // 34種のマスターリスト
    private List<Sprite> gameDeck = new List<Sprite>(); // 136枚の山
    private List<GameObject> playerHand = new List<GameObject>(); // 14枚の手牌
    private List<GameObject> candidateTiles = new List<GameObject>(); // 5枚の候補牌

    // --- 選択中リスト ---
    private List<GameObject> selectedTiles = new List<GameObject>(); // 現在のステートで選択中の牌
    private List<GameObject> selectedCandidateTiles = new List<GameObject>(); // Phase1で選択が確定した候補牌

    private MahjongScoring mahjongScoring;

    void Start()
    {
        // 初期設定
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
        // 状態に応じた牌選択の処理
        if (currentState == GameState.SelectCandidates)
        {
            // 候補牌（上段）の選択
            HandleTileSelection(candidateTiles, 5);
        }
        else if (currentState == GameState.SelectHandTiles)
        {
            // 手牌（下段）の選択
            // 候補で選んだ数しか選択できないようにする
            HandleTileSelection(playerHand, selectedCandidateTiles.Count);
        }

        // 画面タッチでリセット
        if ((currentState == GameState.NextTurn || currentState == GameState.GameOver) && Input.GetMouseButtonDown(0))
        {
            ResetAndRestart();
        }
    }

    /// <summary>
    /// メインのUIボタンの表示/非表示を制御
    /// </summary>
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

        int totalTiles = 14;
        float startX = -(totalTiles - 1) * tileSpacing / 2f;
        int displayedCount = 0;

        // 4枚ずつ3回表示
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (displayedCount >= totalTiles) break;
                Sprite tileSprite = DrawTile();
                if (tileSprite == null) yield break;
                GameObject tileObject = CreateTileGameObject(tileSprite, new Vector3(startX + displayedCount * tileSpacing, 0, 0));
                playerHand.Add(tileObject);
                displayedCount++;
            }
            yield return new WaitForSeconds(displayDelay);
        }

        // 最後に2枚表示
        for (int i = 0; i < 2; i++)
        {
            if (displayedCount >= totalTiles) break;
            Sprite tileSprite = DrawTile();
            if (tileSprite == null) yield break;
            GameObject tileObject = CreateTileGameObject(tileSprite, new Vector3(startX + displayedCount * tileSpacing, 0, 0));
            playerHand.Add(tileObject);
            displayedCount++;
        }

        yield return new WaitForSeconds(displayDelay);
        SortAndRedisplayHand(); // 配牌ソート

        // ★変更: 捨て牌モードではなく、候補牌の抽選に移行
        StartCoroutine(DrawCandidateTiles());
    }

    // --------------------------------------------------------------------------------
    // 2. 牌選択モード (汎用)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// 牌選択の共通ロジック
    /// </summary>
    /// <param name="targetList">選択対象のリスト (手牌 or 候補牌)</param>
    /// <param name="selectionLimit">選択できる最大数</param>
    private void HandleTileSelection(List<GameObject> targetList, int selectionLimit)
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedTile = hit.collider.gameObject;

                // 選択対象のリスト(targetList)に含まれている牌かチェック
                if (targetList.Contains(clickedTile))
                {
                    if (selectedTiles.Contains(clickedTile))
                    {
                        // 選択解除
                        clickedTile.transform.position -= new Vector3(0, selectionOffset, 0);
                        selectedTiles.Remove(clickedTile);
                    }
                    // 選択上限(selectionLimit)に達していないかチェック
                    else if (selectedTiles.Count < selectionLimit)
                    {
                        // 新規選択
                        clickedTile.transform.position += new Vector3(0, selectionOffset, 0);
                        selectedTiles.Add(clickedTile);
                    }
                    // else: 上限に達しているので選択不可
                }
            }
        }
    }

    // --------------------------------------------------------------------------------
    // 3. 捨て牌/交換ボタン押下処理 (ステートマシン)
    // --------------------------------------------------------------------------------
    private void OnDiscardButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectCandidates:
                // Phase 1: 候補牌の選択を決定
                ProcessCandidateSelection();
                break;

            case GameState.SelectHandTiles:
                // Phase 2: 手牌の選択を決定 (交換実行)
                ProcessHandTileSelection();
                break;
        }
    }

    /// <summary>
    /// Phase 1: 候補牌の選択を決定したときの処理
    /// </summary>
    private void ProcessCandidateSelection()
    {
        // 選択した牌を `selectedCandidateTiles` リストに退避
        selectedCandidateTiles.Clear();
        foreach (var tile in selectedTiles)
        {
            selectedCandidateTiles.Add(tile);
            // 選択解除 (Y座標を元に戻す)
            tile.transform.position -= new Vector3(0, selectionOffset, 0);
        }
        selectedTiles.Clear(); // メインの選択リストをクリア

        if (selectedCandidateTiles.Count == 0)
        {
            // 0枚選択時: 候補牌を全て捨てて、次の5枚を引く
            StartCoroutine(ProcessDiscardAndSwap());
        }
        else
        {
            // 1〜5枚選択時: 手牌選択フェーズに移行
            currentState = GameState.SelectHandTiles;

            // UI更新
            resultText.text = $"手牌から {selectedCandidateTiles.Count} 枚選択してください";
            resultText.gameObject.SetActive(true);
            discardButtonText.text = "交換";
            sortButton.gameObject.SetActive(false); // 手牌選択中はソート禁止
            winButton.gameObject.SetActive(false);  // 手牌選択中は和了禁止
        }
    }

    /// <summary>
    /// Phase 2: 手牌の選択を決定したときの処理 (交換実行)
    /// </summary>
    private void ProcessHandTileSelection()
    {
        // 候補牌の選択数と手牌の選択数が一致するかチェック
        if (selectedTiles.Count != selectedCandidateTiles.Count)
        {
            // 枚数が違う場合、エラーメッセージ表示
            StartCoroutine(ShowTemporaryMessage("選択枚数が違います"));
            return;
        }

        // 枚数が一致した場合、入れ替え処理を開始
        resultText.gameObject.SetActive(false);
        StartCoroutine(ProcessDiscardAndSwap());
    }

    // --------------------------------------------------------------------------------
    // 4. 牌の描画・交換・破棄 (コルーチン)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// ★新規: 5枚の候補牌を山から引いて表示する
    /// </summary>
    private IEnumerator DrawCandidateTiles()
    {
        currentState = GameState.DrawCandidates;
        SetUIActive(false); // ボタンを一時的に無効化
        resultText.gameObject.SetActive(false);

        // 1. 古い候補牌を破棄 (あれば)
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
        selectedTiles.Clear();

        // 2. 山から5枚引く
        int totalCandidates = 5;
        float startX = -(totalCandidates - 1) * tileSpacing / 2f;

        for (int i = 0; i < totalCandidates; i++)
        {
            Sprite tileSprite = DrawTile();
            if (tileSprite == null)
            {
                // 山が尽きた
                if (candidateTiles.Count == 0)
                {
                    // 1枚も引けなかった -> 流局
                    yield break; // DrawTile() が GameOver 処理を開始済み
                }
                else
                {
                    // 5枚引く前に山が尽きた (これが最後の補充)
                    break;
                }
            }

            // 候補牌を指定座標（手牌の上）に生成
            Vector3 pos = new Vector3(startX + i * tileSpacing, candidateTilePos.y, 0);
            GameObject tileObj = CreateTileGameObject(tileSprite, pos);
            candidateTiles.Add(tileObj);
            yield return new WaitForSeconds(0.1f); // 1枚ずつ表示
        }

        // 3. 候補選択ステートに移行
        currentState = GameState.SelectCandidates;
        SetUIActive(true); // UIを有効化
        discardButtonText.text = "決定";
        resultText.text = "入れ替える牌を選択してください (0枚でも可)";
        resultText.gameObject.SetActive(true);
    }

    /// <summary>
    /// ★新規: 牌の入れ替えと破棄を実行する
    /// </summary>
    private IEnumerator ProcessDiscardAndSwap()
    {
        currentState = GameState.ProcessingSwap;
        SetUIActive(false);
        resultText.gameObject.SetActive(false);

        // --- ケース1: 0枚選択時 (候補牌を全て破棄) ---
        if (selectedCandidateTiles.Count == 0)
        {
            foreach (var tile in candidateTiles)
            {
                // (任意) 破棄アニメーション
                Destroy(tile, 0.5f);
            }
            candidateTiles.Clear();
            yield return new WaitForSeconds(0.6f); // 破棄アニメーション待ち

            // 次の候補牌を引く
            StartCoroutine(DrawCandidateTiles());
            yield break; // このコルーチンは終了
        }

        // --- ケース2: 1〜5枚選択時 (手牌と交換) ---

        // 1. 選択した「手牌」を破棄
        // (この時点で selectedTiles には手牌が入っている)
        foreach (var tile in selectedTiles)
        {
            playerHand.Remove(tile);
            // (任意) 破棄アニメーション
            tile.transform.position += new Vector3(0, 3, 0); // 上に飛んでいく
            Destroy(tile, 0.5f);
        }

        // 2. 選択されなかった「候補牌」を破棄
        foreach (var tile in candidateTiles)
        {
            if (!selectedCandidateTiles.Contains(tile))
            {
                // (任意) 破棄アニメーション
                Destroy(tile, 0.5f);
            }
        }

        // 3. 選択した「候補牌」を手牌リストに追加
        // (この時点で selectedCandidateTiles には候補牌が入っている)
        foreach (var tile in selectedCandidateTiles)
        {
            playerHand.Add(tile);
        }

        // 4. 一時リストを全てクリア
        selectedTiles.Clear();
        selectedCandidateTiles.Clear();
        candidateTiles.Clear();

        yield return new WaitForSeconds(0.6f); // 破棄アニメーション待ち

        // 5. 手牌をソートして再描画
        // (SortAndRedisplayHandは、playerHand内のGameObjectを一旦破棄し、
        // y=0 の位置にソートして再生成する)
        SortAndRedisplayHand();

        yield return new WaitForSeconds(0.5f); // 牌譜確認時間

        // 6. 次の候補牌を引く
        StartCoroutine(DrawCandidateTiles());
    }


    // --------------------------------------------------------------------------------
    // 5. ソートボタン処理
    // --------------------------------------------------------------------------------
    private void OnSortButtonClick()
    {
        // 候補選択中のみソート可能
        if (currentState != GameState.SelectCandidates) return;

        // 選択中の手牌があれば解除
        if (selectedTiles.Count > 0)
        {
            foreach (var tile in selectedTiles)
            {
                tile.transform.position -= new Vector3(0, selectionOffset, 0);
            }
            selectedTiles.Clear();
        }

        // 手牌（下段）のソートを実行
        SortAndRedisplayHand();
    }

    // 6. ソート実行ロジック
    private void SortAndRedisplayHand()
    {
        // (変更なし)
        Array.Clear(tmp_wk, 0, tmp_wk.Length);
        CopyHandToTmp();
        Array.Sort(tmp_wk, 0, playerHand.Count);
        CopyTmpToHand();
    }

    // (旧) RearrangeAndReplenish と ReplenishTile は不要になったので削除

    private IEnumerator SortHand()
    {
        SortAndRedisplayHand();
        yield return null;
    }

    // --------------------------------------------------------------------------------
    // 7. 上がり判定モードと結果表示
    // --------------------------------------------------------------------------------
    private void OnWinButtonClick()
    {
        // 候補牌の選択中のみ「和了」ボタンを有効にする
        if (currentState != GameState.SelectCandidates) return;

        // 何か選択していたら解除
        if (selectedTiles.Count > 0)
        {
            foreach (var tile in selectedTiles)
            {
                tile.transform.position -= new Vector3(0, selectionOffset, 0);
            }
            selectedTiles.Clear();
        }

        currentState = GameState.WinCheck;
        SetUIActive(false);
        resultText.gameObject.SetActive(false);
        // 候補牌も非表示にする
        foreach (var tile in candidateTiles) { tile.SetActive(false); }

        StartCoroutine(CheckWinCondition());
    }

    private IEnumerator CheckWinCondition()
    {
        // (変更なし)
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
    // 8. 次のターン
    // --------------------------------------------------------------------------------
    private void ResetAndRestart()
    {
        // 既存の牌をすべて削除
        foreach (GameObject tile in playerHand) { Destroy(tile); }
        foreach (GameObject tile in candidateTiles) { Destroy(tile); }
        playerHand.Clear();
        candidateTiles.Clear();
        selectedTiles.Clear();
        selectedCandidateTiles.Clear();

        // 山を再構築
        BuildDeck();

        // ゲームを最初から再開
        StartCoroutine(StartGame());
    }

    // --------------------------------------------------------------------------------
    // 9. ユーティリティ関数
    // --------------------------------------------------------------------------------

    /// <summary>
    /// 136枚のゲームデッキを構築し、シャッフルする
    /// </summary>
    private void BuildDeck()
    {
        gameDeck.Clear();
        if (allMahjongTiles == null || allMahjongTiles.Count == 0)
        {
            Debug.LogError("マスター牌リスト(allMahjongTiles)が空です。LoadMahjongTilesを先に実行してください。");
            return;
        }
        foreach (Sprite tileSprite in allMahjongTiles)
        {
            for (int i = 0; i < 4; i++)
            {
                gameDeck.Add(tileSprite);
            }
        }
        gameDeck.Shuffle();
    }

    /// <summary>
    /// デッキ（山）から牌を1枚引く
    /// </summary>
    private Sprite DrawTile()
    {
        // 王牌(14枚)を残す
        if (gameDeck.Count <= 14)
        {
            StartCoroutine(ShowGameOver("（流局）"));
            return null;
        }

        Sprite tile = gameDeck[0];
        gameDeck.RemoveAt(0);
        return tile;
    }

    /// <summary>
    /// ゲームオーバー（流局）処理
    /// </summary>
    private IEnumerator ShowGameOver(string message)
    {
        if (currentState == GameState.GameOver) yield break;
        currentState = GameState.GameOver;
        resultText.text = "ゲームオーバー\n" + message;
        resultText.gameObject.SetActive(true);
        SetUIActive(false);
        // 候補牌も消す
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
    }

    /// <summary>
    /// 一時的なメッセージを表示
    /// </summary>
    private IEnumerator ShowTemporaryMessage(string message)
    {
        resultText.text = message;
        resultText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        // 現在のステートがまだ手牌選択中なら、ガイダンスを再表示
        if (currentState == GameState.SelectHandTiles)
        {
            resultText.text = $"手牌から {selectedCandidateTiles.Count} 枚選択してください";
        }
        else
        {
            resultText.gameObject.SetActive(false);
        }
    }

    private GameObject CreateTileGameObject(Sprite sprite, Vector3 position)
    {
        // (変更なし)
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
        // (変更なし)
        allMahjongTiles = new List<Sprite>();
        for (int i = 1; i <= 9; i++)
        {
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_m_{i}"));
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_p_{i}"));
            allMahjongTiles.Add(Resources.Load<Sprite>($"MahjongTiles/mjpai_s_{i}"));
        }
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_t")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_n"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_s")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_p"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_w")); allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_g"));
        allMahjongTiles.Add(Resources.Load<Sprite>("MahjongTiles/mjpai_j_r"));
        allMahjongTiles.RemoveAll(item => item == null);
        if (allMahjongTiles.Count != 34)
        {
            Debug.LogError($"麻雀牌のスプライトの読み込みに失敗しました。34種類必要ですが、{allMahjongTiles.Count}種類しか読み込めませんでした。");
        }
    }

    private int[] tmp_wk = new int[14];

    private void CopyHandToTmp()
    {
        // (変更なし)
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
        // (変更なし)
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
    // (変更なし)
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