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
        HandOut,            // 1. 配牌 (13枚)
        DrawRuleA,          // 2. [A] 5枚の候補牌を引く
        SelectRuleACandidates, // 3. [A] 候補牌を選択中 (N枚)
        SelectRuleAHand,    // 4. [A] 手牌を選択中 (M枚)
        ProcessingRuleA,    // 5. [A] 入れ替え/破棄を実行中
        DrawRuleB,          // 6. [B] 3枚の候補牌(鳴き)を引く
        SelectRuleBAction,  // 7. [B] 鳴き/パス/ロンの選択
        SelectRuleBWinTile, // 8. [B] ロン牌を選択
        WinCheck,           // 9. 上がり判定 (A or B)
        Result,             // 10. 結果表示
        NextTurn,           // 11. 次のターン準備
        GameOver            // 12. ゲームオーバー
    }

    private GameState currentState = GameState.HandOut;

    // --- UI関連 (Inspectorで設定) ---
    public Button discardButton;
    public Button winButton;
    public Button sortButton;
    public TextMeshProUGUI resultText;

    // ★★★ Inspectorで設定が必要 ★★★
    public TextMeshProUGUI discardButtonText;
    public TextMeshProUGUI winButtonText;
    public TextMeshProUGUI sortButtonText;
    // ★★★★★★★★★★★★★★★★★★★
    public Toggle nakiModeToggle; // 鳴きモード切り替えトグル
    // --- 牌のデータと設定 ---
    public float tileSpacing = 1.3f;
    public float displayDelay = 0.5f;
    public float selectionOffset = 0.7f;
    public float tileScale = 0.5f;
    private Vector3 candidateTilePos = new Vector3(0, 2.5f, 0); // 候補牌のY座標

    // --- 牌リスト ---
    private List<Sprite> allMahjongTiles; // 34種のマスターリスト
    private List<Sprite> gameDeck = new List<Sprite>(); // 136枚の山
    private List<GameObject> playerHand = new List<GameObject>(); // 手牌 (13 or 14枚)
    private List<GameObject> candidateTiles = new List<GameObject>(); // 候補牌 (5枚 or 3枚)

    // --- 選択中リスト ---
    private List<GameObject> selectedTiles = new List<GameObject>(); // 現在のステートで選択中の牌
    private List<GameObject> selectedCandidateTiles = new List<GameObject>(); // [A]で選択が確定した候補牌

    private MahjongScoring mahjongScoring;

    void Start()
    {
        // 初期設定
        LoadMahjongTiles(); // 34種のマスターをロード
        BuildDeck();        // 136枚の山を構築
        SetUIActive(false, false, false);
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
        if (currentState == GameState.SelectRuleACandidates)
        {
            // [A] 候補牌（5枚）の選択
            HandleTileSelection(candidateTiles, 5);
        }
        else if (currentState == GameState.SelectRuleAHand)
        {
            // [A] 手牌の選択
            // N枚 (交換) または N-1枚 (ツモ)
            int N = selectedCandidateTiles.Count;
            int limit = Mathf.Max(N, N - 1);
            HandleTileSelection(playerHand, limit, false); // 複数選択を許可
        }
        else if (currentState == GameState.SelectRuleBWinTile)
        {
            // [B] ロン牌の選択 (1枚のみ)
            HandleTileSelection(candidateTiles, 1, true); // 1枚のみ
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
    private void SetUIActive(bool discard, bool win, bool sort)
    {
        discardButton.gameObject.SetActive(discard);
        winButton.gameObject.SetActive(win);
        sortButton.gameObject.SetActive(sort);
    }

    // --------------------------------------------------------------------------------
    // 1. 配牌モード
    // --------------------------------------------------------------------------------
    private IEnumerator StartGame()
    {
        currentState = GameState.HandOut;
        resultText.gameObject.SetActive(false);

        // ★変更: 13枚配る
        int totalTiles = 13;
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

        // 最後に1枚表示
        if (displayedCount < totalTiles)
        {
            Sprite tileSprite = DrawTile();
            if (tileSprite == null) yield break;
            GameObject tileObject = CreateTileGameObject(tileSprite, new Vector3(startX + displayedCount * tileSpacing, 0, 0));
            playerHand.Add(tileObject);
            displayedCount++;
        }

        yield return new WaitForSeconds(displayDelay);
        SortAndRedisplayHand(); // 配牌ソート

        // [ルールA] ツモ・モードに移行
        StartCoroutine(DrawCandidateTilesA());
    }

    // --------------------------------------------------------------------------------
    // 2. 牌選択モード (汎用)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// 牌選択の共通ロジック
    /// </summary>
    /// <param name="targetList">選択対象のリスト (手牌 or 候補牌)</param>
    /// <param name="selectionLimit">選択できる最大数</param>
    /// <param name="mutuallyExclusive">true=1枚だけ選択(ロン牌用), false=複数選択(交換用)</param>
    private void HandleTileSelection(List<GameObject> targetList, int selectionLimit, bool mutuallyExclusive = false)
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedTile = hit.collider.gameObject;

                if (targetList.Contains(clickedTile))
                {
                    if (selectedTiles.Contains(clickedTile))
                    {
                        // 選択解除
                        clickedTile.transform.position -= new Vector3(0, selectionOffset, 0);
                        selectedTiles.Remove(clickedTile);
                    }
                    else
                    {
                        // 新規選択
                        if (mutuallyExclusive)
                        {
                            // 既存の選択を解除
                            foreach (var tile in selectedTiles)
                            {
                                tile.transform.position -= new Vector3(0, selectionOffset, 0);
                            }
                            selectedTiles.Clear();
                        }

                        if (selectedTiles.Count < selectionLimit)
                        {
                            clickedTile.transform.position += new Vector3(0, selectionOffset, 0);
                            selectedTiles.Add(clickedTile);
                        }
                    }
                }
            }
        }
    }

    // --------------------------------------------------------------------------------
    // 3. ボタン押下処理 (ステートマシン)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// (左) 「決定」「交換」「鳴き」「ロン決定」ボタン
    /// </summary>
    private void OnDiscardButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleACandidates:
                // [A] 候補牌の選択を決定
                ProcessCandidateSelectionA();
                break;
            case GameState.SelectRuleAHand:
                // [A] 手牌の選択を決定 (交換実行)
                ProcessHandTileSelectionA();
                break;
            case GameState.SelectRuleBAction:
                // [B] 「鳴き」ボタン
                ProcessNakiB();
                break;
            case GameState.SelectRuleBWinTile:
                // [B] 「ロン決定」ボタン
                ProcessWinTileSelectionB();
                break;
        }
    }

    /// <summary>
    /// (中) 「ツモ」「ロン」ボタン
    /// </summary>
    private void OnWinButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleAHand:
                // [A] 「ツモ」ボタン (14枚での和了)
                ProcessTsumoWinCheckA();
                break;
            case GameState.SelectRuleBAction:
                // [B] 「ロン」ボタン (13枚 + 3枚での和了)
                ProcessRonCheckB();
                break;
        }
    }

    /// <summary>
    /// (右) 「ソート」「パス」ボタン
    /// </summary>
    private void OnSortButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleACandidates:
            case GameState.SelectRuleAHand:
                // [A] 「ソート」ボタン
                ProcessSortA();
                break;
            case GameState.SelectRuleBAction:
                // [B] 「パス」ボタン
                ProcessPassB();
                break;
        }
    }

    // --------------------------------------------------------------------------------
    // 4. [A] ツモ・モード (5枚交換) のロジック
    // --------------------------------------------------------------------------------

    /// <summary>
    /// [A] 5枚の候補牌を山から引いて表示する
    /// </summary>
    private IEnumerator DrawCandidateTilesA()
    {
        currentState = GameState.DrawRuleA;
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);

        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
        selectedTiles.Clear();

        int totalCandidates = 5;
        float startX = -(totalCandidates - 1) * tileSpacing / 2f;

        for (int i = 0; i < totalCandidates; i++)
        {
            Sprite tileSprite = DrawTile();
            if (tileSprite == null)
            {
                if (candidateTiles.Count == 0) yield break; // 流局
                else break; // 5枚引く前に山が尽きた
            }
            Vector3 pos = new Vector3(startX + i * tileSpacing, candidateTilePos.y, 0);
            GameObject tileObj = CreateTileGameObject(tileSprite, pos);
            candidateTiles.Add(tileObj);
            yield return new WaitForSeconds(0.1f);
        }

        currentState = GameState.SelectRuleACandidates;
        SetUIActive(true, false, true); // 決定, (なし), ソート
        discardButtonText.text = "決定";
        sortButtonText.text = "ソート";
        resultText.text = "入れ替える牌を選択してください (0枚でも可)";
        resultText.gameObject.SetActive(true);
    }

    /// <summary>
    /// [A] Phase 1: 候補牌の選択を決定したときの処理
    /// </summary>
    private void ProcessCandidateSelectionA()
    {
        selectedCandidateTiles.Clear();
        foreach (var tile in selectedTiles)
        {
            selectedCandidateTiles.Add(tile);
            //tile.transform.position -= new Vector3(0, selectionOffset, 0);
        }
        selectedTiles.Clear();

        int N = selectedCandidateTiles.Count;

        if (N == 0)
        {
            // 0枚選択時: 候補牌を全て捨てて、[B]鳴きモードへ
            StartCoroutine(ProcessDiscardAndSwapA(0, 0));
        }
        else
        {
            // 1〜5枚選択時: 手牌選択フェーズに移行
            currentState = GameState.SelectRuleAHand;
            resultText.text = $"手牌から {N} 枚 (交換) または {N - 1} 枚 (ツモ) 選択";
            resultText.gameObject.SetActive(true);

            SetUIActive(true, true, true); // 交換, ツモ, ソート
            discardButtonText.text = "交換";
            winButtonText.text = "ツモ";
            sortButtonText.text = "ソート";
        }
    }

    /// <summary>
    /// [A] Phase 2: 「交換」ボタンの処理 (M == N)
    /// </summary>
    private void ProcessHandTileSelectionA()
    {
        int N = selectedCandidateTiles.Count;
        int M = selectedTiles.Count;

        if (M == N)
        {
            // 枚数が一致した場合、入れ替え処理 (手牌13枚) -> [B]へ
            resultText.gameObject.SetActive(false);
            StartCoroutine(ProcessDiscardAndSwapA(N, M, false));
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage($"交換する場合は手牌から {N} 枚選択してください"));
        }
    }

    /// <summary>
    /// [A] Phase 2: 「ツモ」ボタンの処理 (M == N-1)
    /// </summary>
    private void ProcessTsumoWinCheckA()
    {
        int N = selectedCandidateTiles.Count;
        int M = selectedTiles.Count;

        if (N == 0)
        {
            StartCoroutine(ShowTemporaryMessage("ツモ和了は交換する牌が1枚以上必要です"));
            return;
        }

        if (M == N - 1)
        {
            // 枚数が一致した場合、和了判定 (手牌14枚)
            currentState = GameState.WinCheck;
            SetUIActive(false, false, false);
            resultText.gameObject.SetActive(false);
            StartCoroutine(CheckWinConditionA(N, M));
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage($"ツモ和了する場合は手牌から {N - 1} 枚選択してください"));
        }
    }

/// <summary>
    /// [A] 和了判定 (14枚の手牌を仮想的に作って判定)
    /// </summary>
    private IEnumerator CheckWinConditionA(int N, int M)
    {
        // 1. 仮想の手牌(14枚)を作成
        List<GameObject> tempHand = new List<GameObject>(playerHand);
        foreach (var tile in selectedTiles) { tempHand.Remove(tile); } // M枚(N-1枚)除く
        foreach (var tile in selectedCandidateTiles) { tempHand.Add(tile); } // N枚加える
        
        // 2. 判定
        CopyHandToTmp(tempHand); // 14枚の仮想手牌をtmp_wkに
        int winningTile = tmp_wk[13]; // ソート後の最後の牌
        
        // ★ 修正: isRon: false を渡す
        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1, false); // false = ツモ和了

        if (yakuNames.Count > 0)
        {
            // 和了！ -> 実際の入れ替え処理を実行
            // ★ 修正: yakuNames を ProcessDiscardAndSwapA に渡す
            StartCoroutine(ProcessDiscardAndSwapA(N, M, true, yakuNames));
        }
        else
        {
            // 上がりなし
            StartCoroutine(ShowTemporaryMessage("上がりなし"));
            currentState = GameState.SelectRuleAHand; // 手牌選択に戻る
            SetUIActive(true, true, true); // ボタン再表示
        }
        yield return null;
    }    /// <summary>
    /// [A] 和了判定 (14枚の手牌を仮想的に作って判定)
    /// </summary>
    /*private IEnumerator CheckWinConditionA(int N, int M)
    {
        // 1. 仮想の手牌(14枚)を作成
        List<GameObject> tempHand = new List<GameObject>(playerHand);
        foreach (var tile in selectedTiles) { tempHand.Remove(tile); } // M枚(N-1枚)除く
        foreach (var tile in selectedCandidateTiles) { tempHand.Add(tile); } // N枚加える

        // 2. 判定
        CopyHandToTmp(tempHand); // 14枚の仮想手牌をtmp_wkに
        int winningTile = tmp_wk[13]; // ソート後の最後の牌

        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1);

        if (yakuNames.Count > 0)
        {
            // 和了！ -> 実際の入れ替え処理を実行
            StartCoroutine(ProcessDiscardAndSwapA(N, M, true, yakuNames));
        }
        else
        {
            // 上がりなし
            StartCoroutine(ShowTemporaryMessage("上がりなし"));
            currentState = GameState.SelectRuleAHand; // 手牌選択に戻る
            SetUIActive(true, true, true); // ボタン再表示
        }
        yield return null;
    }*/

    /// <summary>
    /// [A] 牌の入れ替えと破棄を実行する
    /// </summary>
    private IEnumerator ProcessDiscardAndSwapA(int N, int M, bool isWin = false, List<string> yakuNames = null)
    {
        currentState = GameState.ProcessingRuleA;
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);

        // 1. 選択した「手牌」(M枚)を破棄
        foreach (var tile in selectedTiles)
        {
            playerHand.Remove(tile);
            Destroy(tile, 0.5f);
        }

        // 2. 選択されなかった「候補牌」(5-N枚)を破棄
        foreach (var tile in candidateTiles)
        {
            if (!selectedCandidateTiles.Contains(tile))
            {
                Destroy(tile, 0.5f);
            }
        }

        // 3. 選択した「候補牌」(N枚)を手牌リストに追加
        foreach (var tile in selectedCandidateTiles)
        {
            playerHand.Add(tile);
        }

        // 4. 一時リストを全てクリア
        selectedTiles.Clear();
        selectedCandidateTiles.Clear();
        candidateTiles.Clear();

        yield return new WaitForSeconds(0.6f); // 破棄アニメーション待ち

        // 5. 手牌をソートして再描画 (13+N-M 枚になる)
        SortAndRedisplayHand();
        yield return new WaitForSeconds(0.5f);

        if (isWin)
        {
            // 6. 和了結果を表示
            StartCoroutine(ShowWinResult(yakuNames));
        }
        else
        {
            // 6. トグル状態に応じて次のモードへ
            if (nakiModeToggle != null && nakiModeToggle.isOn)
            {
                // [ルールB] 鳴きモードへ移行
                StartCoroutine(DrawCandidateTilesB());
            }
            else
            {
                // [ルールA] ツモ・モードへ戻る (鳴きモードスキップ)
                StartCoroutine(DrawCandidateTilesA());
            }
        }
    }

    /// <summary>
    /// [A] 「ソート」ボタンの処理
    /// </summary>
    private void ProcessSortA()
    {
        // 選択中の牌を解除
        foreach (var tile in selectedTiles)
        {
            tile.transform.position -= new Vector3(0, selectionOffset, 0);
        }
        selectedTiles.Clear();

        // 手牌（下段）のソートを実行
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


    // --------------------------------------------------------------------------------
    // 5. [B] 鳴き・モード (3枚) のロジック
    // --------------------------------------------------------------------------------

    /// <summary>
    /// [B] 3枚の候補牌(鳴き)を山から引いて表示する
    /// </summary>
    private IEnumerator DrawCandidateTilesB()
    {
        currentState = GameState.DrawRuleB;
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);
        selectedTiles.Clear();

        // 1. 古い候補牌を破棄 (あれば)
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();

        // 2. 山から3枚引く
        int totalCandidates = 3;
        float startX = -(totalCandidates - 1) * tileSpacing / 2f;

        for (int i = 0; i < totalCandidates; i++)
        {
            Sprite tileSprite = DrawTile();
            if (tileSprite == null)
            {
                if (candidateTiles.Count == 0) yield break;
                else break;
            }
            Vector3 pos = new Vector3(startX + i * tileSpacing, candidateTilePos.y, 0);
            GameObject tileObj = CreateTileGameObject(tileSprite, pos);
            candidateTiles.Add(tileObj);
            yield return new WaitForSeconds(0.1f);
        }

        // 3. 鳴き選択ステートに移行
        currentState = GameState.SelectRuleBAction;
        SetUIActive(true, true, true); // 鳴き, ロン, パス
        discardButtonText.text = "鳴き";
        winButtonText.text = "ロン";
        sortButtonText.text = "パス";
    }

    /// <summary>
    /// [B] 「パス」ボタンの処理
    /// </summary>
    private void ProcessPassB()
    {
        currentState = GameState.ProcessingRuleA; // 処理中
        SetUIActive(false, false, false);

        // 候補牌(3枚)を破棄
        foreach (var tile in candidateTiles)
        {
            Destroy(tile, 0.5f);
        }
        candidateTiles.Clear();

        // [A] ツモ・モードに戻る
        StartCoroutine(DrawCandidateTilesA());
    }

    /// <summary>
    /// [B] 「鳴き」ボタンの処理
    /// </summary>
    private void ProcessNakiB()
    {
        StartCoroutine(ShowTemporaryMessage("鳴き機能は準備中です"));
    }

    /// <summary>
    /// [B] 「ロン」ボタンの処理 (Phase 1: 聴牌チェック)
    /// </summary>
    private void ProcessRonCheckB()
    {
        // 13枚の手牌で、候補3枚のうちどれかで和了れるかチェック
        bool canWin = false;
        CopyHandToTmp(playerHand); // 13枚の手牌をコピー
        int[] handBase = (int[])tmp_wk.Clone(); // 13枚の手牌を保持

        foreach (var tileObj in candidateTiles)
        {
            int winningTile = GetTileCode(tileObj);
            handBase[13] = winningTile; // 14枚目としてセット

            // MahjongLogic.csのCheckWinは14枚の完成形を渡す必要がある
            if (mahjongScoring.CheckWin(handBase, winningTile, 1, 1,false).Count > 0)
            {
                canWin = true;
                break;
            }
        }

        if (canWin)
        {
            // 当たり牌を選択するフェーズに移行
            currentState = GameState.SelectRuleBWinTile;
            resultText.text = "当たり牌を1枚選択してください";
            resultText.gameObject.SetActive(true);
            SetUIActive(true, false, false); // ロン決定, (なし), (なし)
            discardButtonText.text = "ロン決定";
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage("ロン上がりできません"));
        }
    }

    /// <summary>
    /// [B] 「ロン決定」ボタンの処理 (Phase 2: 当たり牌選択)
    /// </summary>
    private void ProcessWinTileSelectionB()
    {
        if (selectedTiles.Count == 1)
        {
            // 選択した牌で上がり判定
            currentState = GameState.WinCheck;
            SetUIActive(false, false, false);
            resultText.gameObject.SetActive(false);
            StartCoroutine(CheckWinConditionB());
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage("当たり牌を1枚選んでください"));
        }
    }
/// <summary>
    /// [B] 和了判定 (13枚の手牌 + 選択した1枚)
    /// </summary>
    private IEnumerator CheckWinConditionB()
    {
        // 1. 手牌(13枚) + 選択牌(1枚)で14枚の手牌を作成
        CopyHandToTmp(playerHand);
        GameObject winningTileObj = selectedTiles[0];
        int winningTile = GetTileCode(winningTileObj);
        tmp_wk[13] = winningTile; // 14枚目としてセット
        
        // ★ 修正: isRon: true を渡す
        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1, true); // true = ロン和了

        if (yakuNames.Count > 0)
        {
            // 和了！
            // 当たり牌を手牌に加え、他を破棄
            playerHand.Add(winningTileObj);
            candidateTiles.Remove(winningTileObj);
            foreach (var tile in candidateTiles) { Destroy(tile); }
            candidateTiles.Clear();
            selectedTiles.Clear();

            SortAndRedisplayHand();
            
            // ★ 修正: yakuNames を ShowWinResult に渡す
            StartCoroutine(ShowWinResult(yakuNames));
        }
        else
        {
            // ... (上がりなしの処理) ...
        }
        yield return null;
    }
    /// <summary>
    /// [B] 和了判定 (13枚の手牌 + 選択した1枚)
    /// </summary>
    /*private IEnumerator CheckWinConditionB()
    {
        // 1. 手牌(13枚) + 選択牌(1枚)で14枚の手牌を作成
        CopyHandToTmp(playerHand);
        GameObject winningTileObj = selectedTiles[0];
        int winningTile = GetTileCode(winningTileObj);
        tmp_wk[13] = winningTile; // 14枚目としてセット

        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1);

        if (yakuNames.Count > 0)
        {
            // 和了！
            // 当たり牌を手牌に加え、他を破棄
            playerHand.Add(winningTileObj);
            candidateTiles.Remove(winningTileObj);
            foreach (var tile in candidateTiles) { Destroy(tile); }
            candidateTiles.Clear();
            selectedTiles.Clear();

            SortAndRedisplayHand();
            StartCoroutine(ShowWinResult(yakuNames));
        }
        else
        {
            // 上がりなし (選択ミス)
            StartCoroutine(ShowTemporaryMessage("上がりなし (選択が違います)"));
            // 鳴き選択モードに戻る
            selectedTiles.Clear();
            winningTileObj.transform.position -= new Vector3(0, selectionOffset, 0); // 選択解除
            StartCoroutine(DrawCandidateTilesB()); // 3枚表示に戻す (実際は破棄せず再表示)
        }
        yield return null;
    }*/

    // --------------------------------------------------------------------------------
    // 6. 結果表示・リセット
    // --------------------------------------------------------------------------------

    /// <summary>
    /// 和了結果を表示
    /// </summary>
 /*   private IEnumerator ShowWinResult(List<string> yakuNames)
    {
        currentState = GameState.Result;
        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false); // ★追加
        // 候補牌が残っていれば破棄
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
        selectedTiles.Clear();

        // 役表示
        resultText.text = string.Join("\n", yakuNames);
        resultText.gameObject.SetActive(true);
        currentState = GameState.NextTurn;
        yield return null;
    }*/
/// <summary>
    /// 和了結果を表示
    /// </summary>
    private IEnumerator ShowWinResult(List<string> yakuNames)
    {
        currentState = GameState.Result;
        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false); 
        
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
        selectedTiles.Clear();

        // ★ 修正: 役名リスト + 点数サマリ を表示
        
        // 1. 点数サマリを取得 (Logic側で親/子を自動判定)
        string scoreSummary = mahjongScoring.GetScoreSummary(); 

        // 2. 役名を連結
        string yakuList = string.Join("\n", yakuNames);
        
        // 3. 結合して表示 (C++ の表示形式)
        resultText.text = $"{yakuList}\n\n{scoreSummary}";
        resultText.gameObject.SetActive(true);
        
        currentState = GameState.NextTurn;
        yield return null;
    }

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
        // ★追加: トグルを再表示
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(true);
        // ゲームを最初から再開
        StartCoroutine(StartGame());
    }

    // --------------------------------------------------------------------------------
    // 7. ユーティリティ関数
    // --------------------------------------------------------------------------------

    private void BuildDeck()
    {
        gameDeck.Clear();
        if (allMahjongTiles == null || allMahjongTiles.Count == 0)
        {
            Debug.LogError("マスター牌リスト(allMahjongTiles)が空です。");
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

    private Sprite DrawTile()
    {
        if (gameDeck.Count <= 14)
        {
            StartCoroutine(ShowGameOver("（流局）"));
            return null;
        }
        Sprite tile = gameDeck[0];
        gameDeck.RemoveAt(0);
        return tile;
    }

    private IEnumerator ShowGameOver(string message)
    {
        if (currentState == GameState.GameOver) yield break;
        currentState = GameState.GameOver;
        resultText.text = "ゲームオーバー\n" + message;
        resultText.gameObject.SetActive(true);
        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false); // ★追加
        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();
    }

    private IEnumerator ShowTemporaryMessage(string message)
    {
        resultText.text = message;
        resultText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        // 現在のステートに戻す
        if (currentState == GameState.SelectRuleAHand)
        {
            int N = selectedCandidateTiles.Count;
            resultText.text = $"手牌から {N} 枚 (交換) または {N - 1} 枚 (ツモ) 選択";
        }
        else if (currentState == GameState.SelectRuleBWinTile)
        {
            resultText.text = "当たり牌を1枚選択してください";
        }
        else
        {
            resultText.gameObject.SetActive(false);
        }
    }

    private GameObject CreateTileGameObject(Sprite sprite, Vector3 position)
    {
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

    /// <summary>
    /// GameObjectから牌コードを取得
    /// </summary>
    private int GetTileCode(GameObject tileObj)
    {
        SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return 0;

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
        return value;
    }

    /// <summary>
    /// デフォルト (playerHand) をtmp_wkにコピー
    /// </summary>
    private void CopyHandToTmp()
    {
        CopyHandToTmp(playerHand);
    }

    /// <summary>
    /// 指定したリストをtmp_wkにコピー
    /// </summary>
    private void CopyHandToTmp(List<GameObject> hand)
    {
        Array.Clear(tmp_wk, 0, tmp_wk.Length);
        for (int i = 0; i < hand.Count && i < tmp_wk.Length; i++)
        {
            tmp_wk[i] = GetTileCode(hand[i]);
        }
    }

    private void CopyTmpToHand()
    {
        // playerHand を tmp_wk (ソート済み) を元に再生成する
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