using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using MahjongLogic;

public class MahjongDisplayManager : MonoBehaviour
{
    // ★仕様変更: ゲームの状態を全面的に見直し
    private enum GameState
    {
        // --- ゲーム進行制御 ---
        KyokuStart,       // 1. 局の開始 ("東1局" や目標点数を表示)
        LevelUpDisplay,   // 2. レベルアップ表示
        RyuukyokuDisplay, // 3. 流局表示 (5ターン経過)
        GameClearDisplay, // 4. ゲームクリア (8局終了)
        NextTurn,         // 5. 次の局へ (タップ待ち)
        GameOver,         // 6. ゲームオーバー (未使用)

        // --- [A] ツモ・モード ---
        DrawRuleA,             // [A] 5枚の候補牌を引く
        SelectRuleACandidates, // [A] 候補牌を選択中 (N枚)
        SelectRuleAHand,       // [A] 手牌を選択中 (M枚)
        ProcessingRuleA,       // [A] 入れ替え/破棄を実行中

        // --- [B] 鳴き・モード ---
        DrawRuleB,          // [B] 3枚の候補牌(鳴き)を引く
        SelectRuleBAction,  // [B] 鳴き/パス/ロンの選択
        SelectRuleBWinTile, // [B] ロン牌を選択

        // --- 判定 ---
        WinCheck,           // 上がり判定 (A or B)
        ResultDisplay,      // 上がり役と点数を表示
    }

    private GameState currentState = GameState.KyokuStart;

    // --- UI関連 (Inspectorで設定) ---
    public Button discardButton;
    public Button winButton;
    public Button sortButton;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI discardButtonText;
    public TextMeshProUGUI winButtonText;
    public TextMeshProUGUI sortButtonText;
    public Toggle nakiModeToggle;
    public TextMeshProUGUI gameInfoText; // ★新規: 局・ターン・点数を表示するText

    // --- 牌のデータと設定 ---
    public float tileSpacing = 1.3f;
    public float displayDelay = 0.5f;
    public float selectionOffset = 0.7f;
    public float tileScale = 0.5f;
    private Vector3 candidateTilePos = new Vector3(0, 2.5f, 0);

    // --- 牌リスト ---
    private List<Sprite> allMahjongTiles;
    private List<Sprite> gameDeck = new List<Sprite>();
    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> candidateTiles = new List<GameObject>();
    private List<GameObject> selectedTiles = new List<GameObject>();
    private List<GameObject> selectedCandidateTiles = new List<GameObject>();

    private MahjongScoring mahjongScoring;

    // ★新規: ゲーム進行管理変数
    private int currentLevel = 1;
    private int currentKyoku = 1; // 1-8 (1=E1, 5=S1)
    private int currentTurn = 0;  // 1-5 (局内のターン)
    private int totalScore = 0;
    private const int MAX_TURNS_PER_KYOKU = 5;
    private const int MAX_KYOKU = 8;
    // 局の名前 (0はダミー)
    private string[] kyokuNames = { "", "東1局", "東2局", "東3局", "東4局", "南1局", "南2局", "南3局", "南4局" };


    void Start()
    {
        // 初期設定
        LoadMahjongTiles();
        mahjongScoring = new MahjongScoring();

        // UI初期化
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);
        gameInfoText.text = "";

        // ボタンにイベントリスナーを追加
        discardButton.onClick.AddListener(OnDiscardButtonClick);
        winButton.onClick.AddListener(OnWinButtonClick);
        sortButton.onClick.AddListener(OnSortButtonClick);

        // ★変更: ゲーム(局)開始
        currentState = GameState.KyokuStart;
        StartCoroutine(StartNewKyoku());
    }

    void Update()
    {
        // 状態に応じた牌選択の処理
        if (currentState == GameState.SelectRuleACandidates)
        {
            HandleTileSelection(candidateTiles, 5);
        }
        else if (currentState == GameState.SelectRuleAHand)
        {
            int N = selectedCandidateTiles.Count;
            int limit = Mathf.Max(N, N - 1);
            HandleTileSelection(playerHand, limit, false);
        }
        else if (currentState == GameState.SelectRuleBWinTile)
        {
            HandleTileSelection(candidateTiles, 1, true);
        }


        // 画面タッチでリセット/次へ
        if (currentState == GameState.NextTurn && Input.GetMouseButtonDown(0))
        {
            HandleNextKyoku();
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
    // 1. ゲーム進行 (局・ターン・レベル)
    // --------------------------------------------------------------------------------

    /// <summary>
    /// ★新規: 局の開始 (東1局 開始...など)
    /// </summary>
    private IEnumerator StartNewKyoku()
    {
        currentState = GameState.KyokuStart;
        currentTurn = 0;
        ClearOldHand(); // 牌をクリアし、山を再構築

        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(true);
        resultText.gameObject.SetActive(true);

        string kyokuName = kyokuNames[currentKyoku];
        string displayText = $"{kyokuName} 開始";

        // 東1局の場合のみ目標点数を表示
        if (currentKyoku == 1)
        {
            int targetScore = 5000 + (currentLevel * 1000);
            displayText += $"\n目標点数: {targetScore}点";
        }

        resultText.text = displayText;
        UpdateGameInfoText();

        yield return new WaitForSeconds(2.5f);
        resultText.gameObject.SetActive(false);

        // 13枚配る
        StartCoroutine(DealHand());
    }

    /// <summary>
    /// ★新規: 13枚の配牌を行う
    /// </summary>
    private IEnumerator DealHand()
    {
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
        }

        yield return new WaitForSeconds(displayDelay);
        SortAndRedisplayHand(); // 配牌ソート

        // 最初のターンを開始
        StartNextTurn();
    }

    /// <summary>
    /// ★新規: 次のターン (1/5 ... 5/5) を開始する
    /// </summary>
    private void StartNextTurn()
    {
        currentTurn++;
        UpdateGameInfoText();

        if (currentTurn > MAX_TURNS_PER_KYOKU)
        {
            // 5ターン終了 -> 流局
            StartCoroutine(ShowRyuukyoku());
        }
        else
        {
            // [A] ツモ・モードを開始
            StartCoroutine(DrawCandidateTilesA());
        }
    }

    /// <summary>
    /// ★新規: 流局処理
    /// </summary>
    private IEnumerator ShowRyuukyoku()
    {
        currentState = GameState.RyuukyokuDisplay;
        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false);

        // 牌を消す
        ClearOldHand();

        resultText.text = "流局";
        resultText.gameObject.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        currentState = GameState.NextTurn; // タップ待ち
    }

    /// <summary>
    /// ★新規: 局が終了し、次の局へ進む処理 (タップで呼び出される)
    /// </summary>
    private void HandleNextKyoku()
    {
        currentKyoku++;

        if (currentKyoku > MAX_KYOKU)
        {
            // 全8局が終了 -> レベルアップ判定
            CheckLevelUp();
        }
        else
        {
            // 次の局へ
            StartCoroutine(StartNewKyoku());
        }
    }

    /// <summary>
    /// ★新規: レベルアップ判定
    /// </summary>
    private void CheckLevelUp()
    {
        int targetScore = 5000 + (currentLevel * 1000);

        if (totalScore >= targetScore)
        {
            // クリア！
            currentLevel++;
            StartCoroutine(ShowLevelUp());
        }
        else
        {
            // ゲームオーバー (今回はクリアとして扱う)
            StartCoroutine(ShowGameClear(false));
        }
    }

    /// <summary>
    /// ★新規: レベルアップ表示
    /// </summary>
    private IEnumerator ShowLevelUp()
    {
        currentState = GameState.LevelUpDisplay;
        SetUIActive(false, false, false);
        resultText.text = $"レベルアップ！\n\nレベル {currentLevel} になりました";
        resultText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3.0f);

        // 次のゲーム (東1局) へ
        currentKyoku = 1;
        totalScore = 0;
        StartCoroutine(StartNewKyoku());
    }

    /// <summary>
    /// ★新規: ゲームクリア (8局終了) 表示
    /// </summary>
    private IEnumerator ShowGameClear(bool levelUp)
    {
        currentState = GameState.GameClearDisplay;
        SetUIActive(false, false, false);
        resultText.text = levelUp ? "レベルアップ！" : "ゲームクリア！";
        resultText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3.0f);

        // 次のゲーム (東1局) へ
        currentKyoku = 1;
        totalScore = 0;
        StartCoroutine(StartNewKyoku());
    }

    /// <summary>
    /// ★新規: 局、ターン、点数情報を更新
    /// </summary>
    private void UpdateGameInfoText()
    {
        if (gameInfoText != null)
        {
            string kyoku = kyokuNames[currentKyoku];
            string turn = (currentTurn > 0) ? $"{currentTurn}/{MAX_TURNS_PER_KYOKU} ターン" : "";
            gameInfoText.text = $"{kyoku}  {turn}\n合計点: {totalScore}点";
        }
    }

    // --------------------------------------------------------------------------------
    // 2. 牌選択モード (汎用)
    // --------------------------------------------------------------------------------

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
                        clickedTile.transform.position -= new Vector3(0, selectionOffset, 0);
                        selectedTiles.Remove(clickedTile);
                    }
                    else
                    {
                        if (mutuallyExclusive)
                        {
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

    private void OnDiscardButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleACandidates:
                ProcessCandidateSelectionA();
                break;
            case GameState.SelectRuleAHand:
                ProcessHandTileSelectionA();
                break;
            case GameState.SelectRuleBAction:
                ProcessNakiB();
                break;
            case GameState.SelectRuleBWinTile:
                ProcessWinTileSelectionB();
                break;
        }
    }

    private void OnWinButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleAHand:
                ProcessTsumoWinCheckA();
                break;
            case GameState.SelectRuleBAction:
                ProcessRonCheckB();
                break;
        }
    }

    private void OnSortButtonClick()
    {
        switch (currentState)
        {
            case GameState.SelectRuleACandidates:
            case GameState.SelectRuleAHand:
                ProcessSortA();
                break;
            case GameState.SelectRuleBAction:
                ProcessPassB();
                break;
        }
    }

    // --------------------------------------------------------------------------------
    // 4. [A] ツモ・モード (5枚交換) のロジック
    // --------------------------------------------------------------------------------

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
                if (candidateTiles.Count == 0) yield break;
                else break;
            }
            Vector3 pos = new Vector3(startX + i * tileSpacing, candidateTilePos.y, 0);
            GameObject tileObj = CreateTileGameObject(tileSprite, pos);
            candidateTiles.Add(tileObj);
            yield return new WaitForSeconds(0.1f);
        }

        currentState = GameState.SelectRuleACandidates;
        SetUIActive(true, false, true);
        discardButtonText.text = "決定";
        sortButtonText.text = "ソート";
        resultText.text = "入れ替える牌を選択してください (0枚でも可)";
        resultText.gameObject.SetActive(true);
    }

    private void ProcessCandidateSelectionA()
    {
        selectedCandidateTiles.Clear();
        foreach (var tile in selectedTiles)
        {
            selectedCandidateTiles.Add(tile);
            // 上にずれたままにする
        }
        selectedTiles.Clear();

        int N = selectedCandidateTiles.Count;

        if (N == 0)
        {
            StartCoroutine(ProcessDiscardAndSwapA(0, 0));
        }
        else
        {
            currentState = GameState.SelectRuleAHand;
            resultText.text = $"手牌から {N} 枚 (交換) または {N - 1} 枚 (ツモ) 選択";
            resultText.gameObject.SetActive(true);

            SetUIActive(true, true, true);
            discardButtonText.text = "交換";
            winButtonText.text = "ツモ";
            sortButtonText.text = "ソート";
        }
    }

    private void ProcessHandTileSelectionA()
    {
        int N = selectedCandidateTiles.Count;
        int M = selectedTiles.Count;

        if (M == N)
        {
            resultText.gameObject.SetActive(false);
            StartCoroutine(ProcessDiscardAndSwapA(N, M, false));
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage($"交換する場合は手牌から {N} 枚選択してください"));
        }
    }

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

    private IEnumerator CheckWinConditionA(int N, int M)
    {
        List<GameObject> tempHand = new List<GameObject>(playerHand);
        foreach (var tile in selectedTiles) { tempHand.Remove(tile); }
        foreach (var tile in selectedCandidateTiles) { tempHand.Add(tile); }

        CopyHandToTmp(tempHand);
        int winningTile = tmp_wk[13];

        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1, false); // false = ツモ和了

        if (yakuNames.Count > 0)
        {
            StartCoroutine(ProcessDiscardAndSwapA(N, M, true, yakuNames));
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage("上がりなし"));
            currentState = GameState.SelectRuleAHand;
            SetUIActive(true, true, true);
        }
        yield return null;
    }

    private IEnumerator ProcessDiscardAndSwapA(int N, int M, bool isWin = false, List<string> yakuNames = null)
    {
        currentState = GameState.ProcessingRuleA;
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);

        foreach (var tile in selectedTiles)
        {
            playerHand.Remove(tile);
            Destroy(tile, 0.5f);
        }

        foreach (var tile in candidateTiles)
        {
            if (!selectedCandidateTiles.Contains(tile))
            {
                Destroy(tile, 0.5f);
            }
        }

        foreach (var tile in selectedCandidateTiles)
        {
            playerHand.Add(tile);
        }

        selectedTiles.Clear();
        selectedCandidateTiles.Clear();
        candidateTiles.Clear();

        yield return new WaitForSeconds(0.6f);

        SortAndRedisplayHand();
        yield return new WaitForSeconds(0.5f);

        if (isWin)
        {
            StartCoroutine(ShowWinResult(yakuNames));
        }
        else
        {
            // ★変更: トグルで分岐
            if (nakiModeToggle != null && nakiModeToggle.isOn)
            {
                StartCoroutine(DrawCandidateTilesB());
            }
            else
            {
                // 鳴きモードOFF -> 次のターンへ
                StartNextTurn();
            }
        }
    }

    private void ProcessSortA()
    {
        foreach (var tile in selectedTiles)
        {
            tile.transform.position -= new Vector3(0, selectionOffset, 0);
        }
        selectedTiles.Clear();
        SortAndRedisplayHand();
    }

    // --------------------------------------------------------------------------------
    // 5. [B] 鳴き・モード (3枚) のロジック
    // --------------------------------------------------------------------------------

    private IEnumerator DrawCandidateTilesB()
    {
        currentState = GameState.DrawRuleB;
        SetUIActive(false, false, false);
        resultText.gameObject.SetActive(false);
        selectedTiles.Clear();

        foreach (var tile in candidateTiles) { Destroy(tile); }
        candidateTiles.Clear();

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

        currentState = GameState.SelectRuleBAction;
        SetUIActive(true, true, true);
        discardButtonText.text = "鳴き";
        winButtonText.text = "ロン";
        sortButtonText.text = "パス";
    }

    private void ProcessPassB()
    {
        currentState = GameState.ProcessingRuleA;
        SetUIActive(false, false, false);

        foreach (var tile in candidateTiles)
        {
            Destroy(tile, 0.5f);
        }
        candidateTiles.Clear();

        // ★変更: [A] ツモ・モードに戻るのではなく、次のターンへ
        StartNextTurn();
    }

    private void ProcessNakiB()
    {
        StartCoroutine(ShowTemporaryMessage("鳴き機能は準備中です"));
    }

    private void ProcessRonCheckB()
    {
        bool canWin = false;
        CopyHandToTmp(playerHand);
        int[] handBase = (int[])tmp_wk.Clone();

        foreach (var tileObj in candidateTiles)
        {
            int winningTile = GetTileCode(tileObj);
            handBase[13] = winningTile;

            // ★変更: isRon: true を渡す
            if (mahjongScoring.CheckWin(handBase, winningTile, 1, 1, true).Count > 0)
            {
                canWin = true;
                break;
            }
        }

        if (canWin)
        {
            currentState = GameState.SelectRuleBWinTile;
            resultText.text = "当たり牌を1枚選択してください";
            resultText.gameObject.SetActive(true);
            SetUIActive(true, false, false);
            discardButtonText.text = "ロン決定";
        }
        else
        {
            StartCoroutine(ShowTemporaryMessage("ロン上がりできません"));
        }
    }

    private void ProcessWinTileSelectionB()
    {
        if (selectedTiles.Count == 1)
        {
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

    private IEnumerator CheckWinConditionB()
    {
        CopyHandToTmp(playerHand);
        GameObject winningTileObj = selectedTiles[0];
        int winningTile = GetTileCode(winningTileObj);
        tmp_wk[13] = winningTile;

        List<string> yakuNames = mahjongScoring.CheckWin(tmp_wk, winningTile, 1, 1, true); // true = ロン和了

        if (yakuNames.Count > 0)
        {
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
            StartCoroutine(ShowTemporaryMessage("上がりなし (選択が違います)"));
            selectedTiles.Clear();
            winningTileObj.transform.position -= new Vector3(0, selectionOffset, 0);
            StartCoroutine(DrawCandidateTilesB());
        }
        yield return null;
    }

    // --------------------------------------------------------------------------------
    // 6. 結果表示・リセット
    // --------------------------------------------------------------------------------

    private IEnumerator ShowWinResult(List<string> yakuNames)
    {
        currentState = GameState.ResultDisplay; // ★変更
        SetUIActive(false, false, false);
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false);

        ClearOldHand(); // ★変更: 牌を全て消す

        // 1. 点数サマリを取得
        string scoreSummary = mahjongScoring.GetScoreSummary();
        // 2. 点数を加算
        totalScore += mahjongScoring.GetScore();
        // 3. 役名を連結
        string yakuList = string.Join("\n", yakuNames);

        // 4. 結合して表示
        resultText.text = $"{yakuList}\n\n{scoreSummary}\n\n合計: {totalScore}点";
        resultText.gameObject.SetActive(true);
        UpdateGameInfoText(); // 合計点をヘッダーにも反映

        currentState = GameState.NextTurn; // タップ待ち
        yield return null;
    }

    /// <summary>
    /// ★変更: ResetAndRestart は不要になったため、ClearOldHand に置き換え
    /// </summary>
    private void ClearOldHand()
    {
        foreach (GameObject tile in playerHand) { Destroy(tile); }
        foreach (GameObject tile in candidateTiles) { Destroy(tile); }
        playerHand.Clear();
        candidateTiles.Clear();
        selectedTiles.Clear();
        selectedCandidateTiles.Clear();

        BuildDeck(); // 山を再構築
    }

    // --------------------------------------------------------------------------------
    // 7. ユーティリティ関数
    // --------------------------------------------------------------------------------

    private void SortAndRedisplayHand()
    {
        CopyHandToTmp();
        Array.Sort(tmp_wk, 0, playerHand.Count);
        CopyTmpToHand();
    }

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
        if (nakiModeToggle != null) nakiModeToggle.gameObject.SetActive(false);
        ClearOldHand();

        yield return new WaitForSeconds(3.0f);
        // ★変更: ゲームオーバー後はレベル1の東1局に戻る
        currentLevel = 1;
        currentKyoku = 1;
        totalScore = 0;
        StartCoroutine(StartNewKyoku());
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

    private void CopyHandToTmp()
    {
        CopyHandToTmp(playerHand);
    }

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