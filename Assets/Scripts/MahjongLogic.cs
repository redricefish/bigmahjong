using System;
using System.Collections.Generic;
using System.Linq;

namespace MahjongLogic
{
    public class MahjongScoring
    {
        // Yaku definitions from MahjongSub.h and yakuname.tbl
        [Flags]
        public enum YakuGroup0 // Yakuman
        {
            Dai3gen = 1 << 0,
            SyoSusiho = 1 << 1,
            Tuiisou = 1 << 2,
            Tinroutou = 1 << 3,
            Ryuisou = 1 << 4,
            Suukantu = 1 << 5,
            Kokusi = 1 << 6,
            Tyuurenpoutou = 1 << 7,
            Tyuurenpoutou13 = 1 << 8,
            Suankou = 1 << 9,
            Tenhou = 1 << 10,
            Lenhou = 1 << 11,
            Tiihou = 1 << 12,
            Suankoutanki = 1 << 13,
            DaiSusiho = 1 << 14,
            Kokusi13 = 1 << 15,
            ShisanPutou = 1 << 16,
        }

        [Flags]
        public enum YakuGroup1 // 1 Han
        {
            Reach = 1 << 0,
            Soku = 1 << 1,
            Pinfu = 1 << 2,
            Ipei = 1 << 3,
            Tanyao = 1 << 4,
            Haitei = 1 << 5,
            Rinsyan = 1 << 6,
            Tumo = 1 << 7,
            Tyankan = 1 << 13,
            Kanburi = 1 << 14,
        }

        [Flags]
        public enum YakuGroup2 // 2 Han
        {
            Dojun = 1 << 0,
            Ituu = 1 << 1,
            Hontyanta = 1 << 2,
            Toitoi = 1 << 3,
            Doupon = 1 << 4,
            Syo3gen = 1 << 5,
            Honrou = 1 << 6,
            Sanankou = 1 << 7,
            Sankantu = 1 << 8,
            Titoitu = 1 << 9,
            Wreach = 1 << 10,
        }

        [Flags]
        public enum YakuGroup3 // 3-6 Han
        {
            Tinitu = 1 << 0,
            Honitu = 1 << 1,
            Ryanpei = 1 << 2,
            Juntyan = 1 << 3,
            Nagasi_mangan = 1 << 4,
        }

        // Yaku names from yakuname.tbl
        private static readonly string[] yaku_wk0_tbl = {
            "大三元", "小四喜", "字一色", "清老頭", "緑一色", "四槓子", "国士無双", "九連宝燈",
            "純正九連宝燈", "四暗刻", "天和", "人和", "地和", "四暗刻単騎", "大四喜", "国士無双１３面", "十三不塔"
        };
        private static readonly string[] yaku_wk3_tbl = { "清一色", "混一色", "二盃口", "純全帯公", "流し満貫" };
        private static readonly string[] yaku_wk2_tbl = { "三色同順", "一気通貫", "全帯公", "対々和", "三色同刻", "小三元", "混老頭", "三暗刻", "三槓子", "七対子", "ダブル立直" };
        private static readonly string[] yaku_wk1_tbl = { "立直", "一発", "平和", "一盃口", "断公九", "海底撈月", "嶺上開花", "自摸和" };
        private static readonly string[] yaku_wk_a_tbl = { "役牌", "ドラ" };

        // Han values from yakuname.tbl
        private static readonly int[] yaku_wk0_han_tb1 = { 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x82, 0x81, 0x81, 0x81, 0x81, 0x82, 0x82, 0x82, 0x81 };
        private static readonly int[] yaku_wk3_han_tb1 = { 6, 3, 3, 3, 5 };
        private static readonly int[] yaku_wk3_han_tb2 = { 5, 2, 0, 2, 0 };
        private static readonly int[] yaku_wk2_han_tb1 = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static readonly int[] yaku_wk2_han_tb2 = { 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static readonly int[] yaku_wk1_han_tb1 = { 1, 1, 1, 1, 1, 1, 1, 1 };

        // Score constants from yakusco.tbl
        private const int oya_yakuten = 48000;
        private const int ko_yakuten = 32000;

        // Score tables from yakusco.tbl
        private static readonly int[] score_tbl = {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            10,11,5,3, 10,11,5,3, 13,15,7,4, 16,16,8,4, 20,20,10,5, 23,24,12,6, 26,27,13,7,
            13,15,7,4, 20,20,10,5, 26,27,13,7, 32,32,16,8, 39,40,20,10, 45,47,23,12, 52,52,26,13,
            26,27,13,7, 39,40,20,10, 52,52,26,13, 64,64,32,16, 77,79,39,20, 80,80,40,20, 80,80,40,20,
            52,52,26,13, 77,79,39,20, 80,80,40,20, 80,80,40,20, 80,80,40,20, 80,80,40,20, 80,80,40,20
        };
        private static readonly int[] score_tbl_0 = {
            80,80,40,20, 120,120,60,30, 120,120,60,30, 160,160,80,40, 160,160,80,40, 160,160,80,40,
            240,240,120,60, 240,240,120,60
        };
        private static readonly int[] score_tbl_1 = {
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            12,12,4,4, 15,15,5,5, 20,21,7,7, 24,24,8,8, 29,30,10,10, 34,36,12,12, 39,39,13,13,
            20,21,7,7, 29,30,10,10, 39,39,13,13, 48,48,16,16, 58,60,20,20, 68,69,23,23, 77,78,26,26,
            39,39,13,13, 58,60,20,20, 77,78,26,26, 96,96,32,32, 116,117,39,39, 120,120,40,40, 120,120,40,40,
            77,78,26,26, 116,117,39,39, 120,120,40,40, 120,120,40,40, 120,120,40,40, 120,120,40,40, 120,120,40,40
        };
        private static readonly int[] score_tbl_2 = {
            120,120,40,40, 180,180,60,60, 180,180,60,60, 240,240,80,80, 240,240,80,80, 240,240,80,80,
            360,360,120,120, 360,360,120,120
        };
        private static readonly int[] ti_scr_tbl_ko = { 16, 16, 8, 4, 32, 32, 16, 8, 64, 64, 32, 16 };
        private static readonly int[] ti_scr_tbl_oya = { 24, 24, 8, 8, 48, 48, 16, 16, 96, 96, 32, 32 };

        // Game state variables that were global in the C++ code
        private int yaku_wk0, yaku_wk1, yaku_wk2, yaku_wk3;
        private int dora_wk, fu_wk, total_han, yakuh_wk;
        private int ykmn_cnt = 0; // ★ ADD THIS
        private int ck_no = 0;    // ★ ADD THIS
        private int kaze0, kaze1; // Wind tiles
        private int[] tmp_wk = new int[14];
        private int[] tmp_wk_v = new int[14];
        private int[] mf_d = new int[mfd_end];
        private int agr_flg;
        private int tmp_ply_flg;
        private int tmp_ronh;
        private const int mfd_end = 15; // 0+1+1+1+4+4+4 = 15


        public bool YakuCheck()
        {
            yk_work_clr();
            kaze_pai_set();

            // This part of the logic needs the result of the hand analysis (ronchk)
            // For now, I will assume the hand analysis has been done and the results are in mf_d, mf_fs, etc.
            // The hand analysis part (ronchk and its helpers) is very complex and will be ported next.

            // A placeholder for the complex hand analysis result
            // (mf_fs[atm_f] | mf_f[atm_f] | mf_fj[atm_f] | mf_fr[atm_f] | mf_fjr[atm_f]);
            int a_f = (mf_fs[atm_f] | mf_f[atm_f] | mf_fj[atm_f] | mf_fr[atm_f] | mf_fjr[atm_f]);

            if ((a_f & AgariFl) == 0)
            {
                // Special hands (Kokushi, Chitoitsu, etc.)
                if ((agr_flg & 2) != 0)
                {
                    kokusi_yk();
                }
                else if ((agr_flg & 4) != 0) // ShisanPutou
                {
                    total_han++;
                    yaku_wk0 |= (int)YakuGroup0.ShisanPutou;
                }
                else
                {
                    // Chitoitsu
                    toitu7_yk();
                    honrou_yk_1();
                    hontin_yk();
                    tanyao_yk();
                    agrpt_yk();
                    tuuiso_yk();
                }

                if ((yaku_wk0 & (int)YakuGroup0.ShisanPutou) == 0)
                {
                    tenhou_yk();
                    if (yaku_wk0 != 0)
                    {
                        yaku_wk1 = yaku_wk2 = yaku_wk3 = 0;
                    }
                }
            }
            else
            {
                // Normal hands
                mf_work_copy();
                yakuman_chk();
                if (yaku_wk0 == 0)
                {
                    han1yk_chk();
                    han2yk_chk();
                    han3yk_chk();
                }
            }

            fu_check();
            // ★ 修正: set_total_han を fu_check の後に移動
            set_total_han();
            if (total_han == 0 && ykmn_cnt == 0) return false;

            if (yaku_wk0 == 0)
            {
                dora_check();
                // ★ 修正: ドラを含めて再度翻計算
                set_total_han();
            }

            return true;
        }

        private void yk_work_clr()
        {
            yaku_wk0 = yaku_wk1 = yaku_wk2 = yaku_wk3 = 0;
            dora_wk = fu_wk = total_han = yakuh_wk = 0;
        }

        private void kaze_pai_set()
        {
            // Placeholder for setting wind tiles based on round and seat
            kaze0 = 0x31; // East
            kaze1 = 0x31; // Player's wind
        }

        private void mf_work_copy()
        {
            // This function copies the correct winning hand combination into mf_d
            if ((mf_fs[atm_f] & AgariFl) != 0)
            {
                Array.Copy(mf_fs, mf_d, mfd_end);
            }
            else if ((mf_f[atm_f] & AgariFl) != 0)
            {
                Array.Copy(mf_f, mf_d, mfd_end);
            }
            else if ((mf_fj[atm_f] & AgariFl) != 0)
            {
                Array.Copy(mf_fj, mf_d, mfd_end);
            }
            else if ((mf_fr[atm_f] & AgariFl) != 0)
            {
                Array.Copy(mf_fr, mf_d, mfd_end);
            }
            else if ((mf_fjr[atm_f] & AgariFl) != 0)
            {
                Array.Copy(mf_fjr, mf_d, mfd_end);
            }
        }

// C++ より移植
        private void set_total_han()
        {
            int i;
            int[] sco_tbl;
            ykmn_cnt = total_han = 0;

            // 役満の翻を計算
            sco_tbl = yaku_wk0_han_tb1; //
            for (i = 0; i < yaku_wk0_tbl.Length; ++i)
            {
                if ((yaku_wk0 & (1 << i)) != 0)
                {
                    total_han += sco_tbl[i];
                    ykmn_cnt += (sco_tbl[i] & 0x3); // ダブル役満(0x82)などを判定
                }
            }

            // 役満の場合、他の翻は足さない (yakuman_chkの後)
            if (ykmn_cnt > 0) return;

            // 3-6翻
            sco_tbl = yaku_wk3_han_tb1; //
            if ((tmp_ply_flg & NomNaki) != 0) sco_tbl = yaku_wk3_han_tb2; // 鳴き
            for (i = 0; i < yaku_wk3_tbl.Length; ++i)
            {
                if ((yaku_wk3 & (1 << i)) != 0)
                {
                    total_han += sco_tbl[i];
                }
            }
            // 2翻
            sco_tbl = yaku_wk2_han_tb1; //
            if ((tmp_ply_flg & NomNaki) != 0) sco_tbl = yaku_wk2_han_tb2; //
            for (i = 0; i < yaku_wk2_tbl.Length; ++i)
            {
                if ((yaku_wk2 & (1 << i)) != 0)
                {
                    total_han += sco_tbl[i];
                }
            }
            // 1翻
            sco_tbl = yaku_wk1_han_tb1; //
            for (i = 0; i < yaku_wk1_tbl.Length; ++i)
            {
                if ((yaku_wk1 & (1 << i)) != 0)
                {
                    total_han += sco_tbl[i];
                }
            }
            total_han += yakuh_wk;
            total_han += dora_wk;
        }
        private void yakuman_chk()
        {
            suukan_yk();
            daisan_yk();
            suusi_yk();
            tuuiso_yk();
            tinrou_yk();
            ryuiso_yk();
            tyuren_yk();
            suuank_yk();
            tenhou_yk();
        }

        private void han1yk_chk()
        {
            tanyao_yk();
            pinf_yk();
            iipei_yk();
            agrpt_yk();
            yakuhi_chk();
        }

        private void han2yk_chk()
        {
            dojun_yk();
            iituu_yk();
            toitoi_yk();
            doupon_yk();
            honrou_yk();
            anko3_yk();
            ren3_iipei_yk();
            kan3_yk();
        }

        private void han3yk_chk()
        {
            hontin_yk();
            ryanpei_yk();
            juntya_yk();
        }

        // ... Ported individual yaku check functions will go here ...
        // Example:
        private void kokusi_yk()
        {
            total_han++;
            if (tanki_chk())
            {
                yaku_wk0 |= (int)YakuGroup0.Kokusi13;
            }
            else
            {
                yaku_wk0 |= (int)YakuGroup0.Kokusi;
            }
        }

        private bool tanki_chk()
        {
            int count = 0;
            for (int i = 0; i < 14; i++)
            {
                // AMask (0xFFFF) 相当
                if ((tmp_ronh & 0xFFFF) == tmp_wk_v[i]) count++;
            }
            return count >= 2;
        }

        private void toitu7_yk()
        {
            yaku_wk2 |= (int)YakuGroup2.Titoitu;
            total_han++;
        }

        private void honrou_yk_1()
        {
            if ((jihai_cnt() + iqhai_cnt()) == 14)
            {
                yaku_wk2 |= (int)YakuGroup2.Honrou;
                total_han++;
            }
        }

        private void hontin_yk()
        {
            int c = same_hai_chk();
            if (c < 0) return;
            if (c == 0)
                yaku_wk3 |= (int)YakuGroup3.Tinitu;
            else
                yaku_wk3 |= (int)YakuGroup3.Honitu;
            total_han++;
        }

        private void agrpt_yk()
        {
            // C++の agrpt_yk からツモ判定を移植
            
            // (kan_flg, tyan_flg, UkoRiip, StReach などの変数が
            // MahjongLogic.cs に移植されていないため、
            // リンシャン、一発、ハイテイなどのロジックは保留します)

            //----ツモチェック (RonNaki=2, NomNaki=4)
            // tmp_ply_flg が 0 (RonNakiでもNomNakiでもない) 場合、ツモとみなす
            // [CheckWin メソッド (line 1114) で tmp_ply_flg = 0 に設定されています]
            if ((tmp_ply_flg & (RonNaki | NomNaki)) == 0)
            {
                yaku_wk1 |= (int)YakuGroup1.Tumo;
                total_han++;
            }
        }


        private void tuuiso_yk()
        {
            if (jihai_cnt() == 14)
            {
                yaku_wk0 |= (int)YakuGroup0.Tuiisou;
                total_han++;
            }
        }

        private void tenhou_yk()
        {
            // Simplified
        }

        private void suukan_yk() { }
        private void daisan_yk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & 0x3F) >= 0x35) c++;
            }

            if (c == 3)
            {
                yaku_wk0 |= (int)YakuGroup0.Dai3gen;
                total_han++;
            }
            else if (c == 2)
            {
                if (mf_d[atm_t] >= 0x35)
                {
                    yaku_wk2 |= (int)YakuGroup2.Syo3gen;
                    total_han++;
                }
            }
        }
        private void suusi_yk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & 0x3F) < 0x35 && (mf_d[ank_t + i] & 0x3F) > 0x30) c++;
            }

            if (c == 4)
            {
                yaku_wk0 |= (int)YakuGroup0.DaiSusiho;
                total_han++;
            }
            else if (c == 3)
            {
                if ((mf_d[atm_t] & 0x3F) < 0x35 && (mf_d[atm_t] & 0x3F) > 0x30)
                {
                    yaku_wk0 |= (int)YakuGroup0.SyoSusiho;
                    total_han++;
                }
            }
        }
        private void tinrou_yk()
        {
            if (iqhai_cnt() == 14)
            {
                yaku_wk0 |= (int)YakuGroup0.Tinroutou;
                total_han++;
            }
        }
        private void ryuiso_yk()
        {
            for (int i = 0; i < 14; i++)
            {
                if (!ryhai_chk(tmp_wk_v[i] & 0x3f)) return;
            }
            yaku_wk0 |= (int)YakuGroup0.Ryuisou;
            total_han++;
        }
        private bool ryhai_chk(int c)
        {
            if (c == 0x36) return true; // Hatsu
            if ((c & 0x30) != 0x20) return false; // Souzu only
            int a = c & 0xf;
            if (a == 1) return false;
            if (a <= 4 || a == 6 || a == 8) return true; // 2,3,4,6,8 Sou
            return false;
        }

        private void tyuren_yk()
        {
            int[] turen_data = { 1, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 9, 9, 0xff };
            int c = tmp_wk_v[0] & 0x30;
            if (c == 0x30) return; // Not a suit

            int j = 0;
            for (int i = 0; i < 14; i++)
            {
                if (tmp_wk_v[i] == (turen_data[j] | c))
                {
                    j++;
                }
            }
            if (j == 13)
            {
                total_han++;
                int count = 0;
                for (int i = 0; i < 14; i++)
                {
                    if ((tmp_ronh & 0xFFFF) == tmp_wk_v[i]) count++;
                }
                if (count == 2 || count == 4)
                {
                    yaku_wk0 |= (int)YakuGroup0.Tyuurenpoutou13;
                }
                else
                {
                    yaku_wk0 |= (int)YakuGroup0.Tyuurenpoutou;
                }
            }
        }
        private void suuank_yk()
        {
            if (mf_d[ank_c] != 4 || (tmp_ply_flg & NomNaki) != 0) return;

            if ((tmp_ply_flg & RonNaki) != 0)
            {
                if (mf_d[atm_t] != (tmp_ronh & 0xFFFF)) return;
            }

            if (mf_d[atm_t] == (tmp_ronh & 0xFFFF))
            {
                yaku_wk0 |= (int)YakuGroup0.Suankoutanki;
            }
            else
            {
                yaku_wk0 |= (int)YakuGroup0.Suankou;
            }
            total_han++;
        }
        private const int NomNaki = (1 << 2);

        private void tanyao_yk()
        {
            // Simplified version
            if ((tmp_ply_flg & NomNaki) != 0) return; // Kuitan check
            if (jihai_cnt() != 0 || iqhai_cnt() != 0) return;
            yaku_wk1 |= (int)YakuGroup1.Tanyao;
            total_han++;
        }

        private void pinf_yk()
        {
            if ((tmp_ply_flg & NomNaki) != 0 || mf_d[jun_c] < 4) return;
            if (!rya_chk()) return;

            // Check if head is a yaku tile
            if (mf_d[atm_t] >= 0x35 || mf_d[atm_t] == kaze0 || mf_d[atm_t] == kaze1) return;

            yaku_wk1 |= (int)YakuGroup1.Pinfu;
            total_han++;
        }

        private bool rya_chk()
        {
            if (rya_chk_0()) return true;
            if (hed_chng() && rya_chk_0()) return true;
            return false;
        }

        private bool rya_chk_0()
        {
            for (int i = 0; i < mf_d[jun_c]; i++)
            {
                if (mf_d[jun_t + i] == (tmp_ronh & 0xFFFF))
                {
                    if ((tmp_ronh & 0xf) != 7) return true;
                }
                else if ((mf_d[jun_t + i] + 2) == (tmp_ronh & 0xFFFF))
                {
                    if ((tmp_ronh & 0xf) != 3) return true;
                }
            }
            return false;
        }

        private void iipei_yk()
        {
            if ((tmp_ply_flg & NomNaki) != 0 || mf_d[jun_c] < 2) return;

            for (int i = 0; i < (mf_d[jun_c] - 1); i++)
            {
                for (int j = i + 1; j < mf_d[jun_c]; j++)
                {
                    if (mf_d[jun_t + i] == mf_d[jun_t + j])
                    {
                        yaku_wk1 |= (int)YakuGroup1.Ipei;
                        total_han++;
                        return;
                    }
                }
            }
        }

        private void yakuhi_chk()
        {
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                int c = mf_d[ank_t + i] & 0x3F;
                if (c > 0x34) // Dragons
                {
                    yakuh_wk++;
                    total_han++;
                }
                else if (c == kaze0 || c == kaze1) // Winds
                {
                    yakuh_wk++;
                    if (kaze0 == kaze1) yakuh_wk++; // Double wind
                    total_han++;
                }
            }
        }
        private const int RonNaki = (1 << 1);
        private const int NakiFl = (1 << 9 | 1 << 10);
        private const int AkanFl = (1 << 8);

        private void dojun_yk()
        {
            if (mf_d[jun_c] < 3) return;
            if (dojun_yk_a()) return;
            if (hed_chng())
            {
                dojun_yk_a();
            }
        }

        private bool dojun_yk_a()
        {
            for (int i = 0; i < (mf_d[jun_c] - 1); i++)
            {
                int d = mf_d[jun_t + i] & 0xf;
                int c = 0;
                for (int j = i + 1; j < mf_d[jun_c]; ++j)
                {
                    if (d == (mf_d[jun_t + j] & 0xf)) ++c;
                }
                if (c >= 2)
                {
                    if (!dojun_yk_20(d)) return false;
                    if (!dojun_yk_20(d + 0x10)) return false;
                    if (!dojun_yk_20(d + 0x20)) return false;
                    yaku_wk2 |= (int)YakuGroup2.Dojun;
                    total_han++;
                    return true;
                }
            }
            return false;
        }

        private bool dojun_yk_20(int cd)
        {
            for (int i = 0; i < mf_d[jun_c]; i++)
            {
                if ((mf_d[jun_t + i] & 0x3f) == cd) return true;
            }
            return false;
        }

        private void iituu_yk()
        {
            if (mf_d[jun_c] < 3) return;
            if (iituu_chk()) return;
            if (hed_chng())
            {
                iituu_chk();
            }
        }

        private bool iituu_chk()
        {
            for (int i = 0; i < (mf_d[jun_c] - 1); ++i)
            {
                int d = mf_d[jun_t + i] & 0x30;
                for (int j = i + 1; j < mf_d[jun_c]; ++j)
                {
                    if (d == (mf_d[jun_t + j] & 0x30))
                    {
                        if (!dojun_yk_20(d + 1)) return false;
                        if (!dojun_yk_20(d + 4)) return false;
                        if (!dojun_yk_20(d + 7)) return false;
                        yaku_wk2 |= (int)YakuGroup2.Ituu;
                        total_han++;
                        return true;
                    }
                }
            }
            return false;
        }

        private void toitoi_yk()
        {
            if (mf_d[ank_c] == 4)
            {
                yaku_wk2 |= (int)YakuGroup2.Toitoi;
                total_han++;
            }
        }
        // TILE CONSTANT (C++のPkJihaに相当)
        private const int PkJiha = 0x31; // 字牌の開始値

        //==================================================
        //
        //		2HAN YAKU CHECKS (移植リクエスト分)
        //
        //==================================================

        //==========doupon check (三色同刻)======================
        private void doupon_yk()
        {
            int i, j, d, e;
            if (mf_d[ank_c] < 3) return;
            for (i = 0; i < (mf_d[ank_c] - 2); ++i)
            {
                if ((mf_d[ank_t + i] & 0x3f) < 0x30) // Check if not an honor tile
                {
                    d = mf_d[ank_t + i] & 0xf; // Get number
                    e = 0;
                    for (j = i + 1; j < mf_d[ank_c]; ++j)
                    {
                        if ((mf_d[ank_t + j] & 0x3f) < 0x30 && (mf_d[ank_t + j] & 0xf) == d)
                        {
                            if (++e >= 2)
                            {
                                yaku_wk2 |= (int)YakuGroup2.Doupon;
                                total_han++;
                                return;
                            }
                        }
                    }
                }
            }
        }

        //===================honroutou (混老頭)=================
        // C++の honrou_yk
        private void honrou_yk()
        {
            // 4面子が刻子 (対々和) かつ、字牌と1,9牌のみで構成されている
            if ((mf_d[ank_c] == 4) && (jihai_cnt() + iqhai_cnt()) == 14)
            {
                yaku_wk2 |= (int)YakuGroup2.Honrou;
                total_han++;
            }
        }

        //===================3ankou (三暗刻)======================
        // C++の anko3_yk
        private void anko3_yk()
        {
            int i, c, a;
            for (c = i = 0; i < mf_d[ank_c]; ++i)
            {
                if ((mf_d[ank_t + i] & NakiFl) == 0) // Check if concealed
                {
                    a = mf_d[ank_t + i] & 0x3f;
                    // ロン和了りの場合、ロン牌が暗刻の成立に影響するかチェック
                    if ((tmp_ply_flg & RonNaki) != 0 && a == (tmp_ronh & 0x3F))
                    {
                        if (!anko_naki(a))
                        {
                            ++c;
                        }
                        else if (anko_ronc(a, i))
                        {
                            ++c;
                        }
                        else if (anko_ronc1(a, i))
                        {
                            ++c;
                        }
                    }
                    else
                    {
                        ++c;
                    }
                }
            }
            if (c == 3)
            {
                yaku_wk2 |= (int)YakuGroup2.Sanankou;
                total_han++;
            }
        }

        // --- anko3_yk の依存関数 ---
        private bool anko_naki(int a)
        {
            int i;
            for (i = 0; i < mf_d[jun_c]; ++i)
            {
                if ((mf_d[jun_t + i] == a) || (mf_d[jun_t + i] + 1) == a || (mf_d[jun_t + i] + 2) == a) return false;
            }
            return true;
        }

        private bool anko_ronc(int a, int ix)
        {
            bool flag = false;
            int i, bb;
            if (a >= 0x30 || (a & 0xf) < 4) return false;

            bb = a - 3;
            if (mf_d[atm_t] != bb) return false;
            for (i = 0; i < mf_d[jun_c]; ++i)
            {
                if (mf_d[jun_t + i] == bb)
                {
                    flag = true; // Can swap
                    break;
                }
            }
            if (flag)
            {
                // Swap
                ++mf_d[jun_t + i];
                mf_d[atm_f] = a;
                mf_d[ank_t + ix] -= 3;
            }
            return flag;
        }

        private bool anko_ronc1(int a, int ix)
        {
            bool flag = false;
            int i, bb;
            if (a >= 0x30 || (a & 0xf) > 6) return false;
            bb = a + 1;
            if (mf_d[atm_t] != (a + 3)) return false;
            for (i = 0; i < mf_d[jun_c]; ++i)
            {
                if (mf_d[jun_t + i] == bb)
                {
                    flag = true; // Can swap
                    break;
                }
            }
            if (flag)
            {
                // Swap
                mf_d[jun_t + i] = a;
                mf_d[atm_f] = a;
                mf_d[ank_t + ix] += 3;
            }
            return flag;
        }
        // --- 依存関数ここまで ---


        //=======三暗刻崩れの一盃口 (C++の ren3_iipei_yk) ======================
        private void ren3_iipei_yk()
        {
            int i, j, d, e;
            // 門前かつ3暗刻
            if ((tmp_ply_flg & NomNaki) != 0 || mf_d[ank_c] != 3) return;
            // 既に三暗刻か一盃口が成立している場合はチェックしない
            if (((yaku_wk2 & (int)YakuGroup2.Sanankou) != 0) || ((yaku_wk1 & (int)YakuGroup1.Ipei) != 0)) return;

            for (i = 0; i < (mf_d[ank_c] - 2); ++i)
            {
                // 数牌の7以下かチェック (111,222,333 のような連刻チェックのため)
                if (((mf_d[ank_t + i] & 0x3f) < 0x30) && ((mf_d[ank_t + i] & 0xf) <= 0x7))
                {
                    e = 0;
                    d = mf_d[ank_t + i] & 0x3f;
                    ++d; // 次の数字
                    for (j = 0; j < mf_d[ank_c]; ++j)
                    {
                        if ((mf_d[ank_t + j] & 0x3f) == d) ++e;
                    }
                    ++d; // さらに次の数字
                    for (j = 0; j < mf_d[ank_c]; ++j)
                    {
                        if ((mf_d[ank_t + j] & 0x3f) == d) ++e;
                    }

                    if (e == 2) // 3つの暗刻が連番になっている (例: 111, 222, 333)
                    {
                        // 本来は三暗刻だが、一盃口x2 (二盃口) としても取れる
                        // C++側では一盃口として処理し、平和のチェックも行っている
                        yaku_wk1 |= (int)YakuGroup1.Ipei;
                        total_han++;

                        // 平和(Pinfu)のチェックのために面子構成を変更
                        mf_d[ank_c] = 0;
                        mf_d[jun_c] = 4;
                        mf_d[jun_t + 1] = mf_d[ank_t];
                        mf_d[jun_t + 2] = mf_d[ank_t];
                        mf_d[jun_t + 3] = mf_d[ank_t];
                        pinf_yk();
                        return;
                    }
                }
            }
        }


        //;====================3kantu (三槓子)======================
        // C++の kan3_yk
        private void kan3_yk()
        {
            int i, c;
            for (c = i = 0; i < mf_d[ank_c]; ++i)
            {
                if ((mf_d[ank_t + i] & AkanFl) != 0) ++c; // AkanFlは暗槓・明槓問わずカン成立フラグ
            }
            if (c == 3)
            {
                yaku_wk2 |= (int)YakuGroup2.Sankantu;
                total_han++;
            }
        }


        //=============================================
        //=
        //=	符 (FU) COUNT
        //=
        //==============================================
        private void fu_check()
        {
            fu_wk = 25;
            if ((yaku_wk2 & (int)YakuGroup2.Titoitu) != 0) return; // 七対子は25符固定

            fu_wk = 20; // 符底 (基本符)

            // 門前ロン (メンゼンカ符)
            if (((tmp_ply_flg & NomNaki) == 0) && ((tmp_ply_flg & RonNaki) != 0))
                fu_wk = 30;

            // ツモ符 (平和(ピンフ)以外)
            if ((yaku_wk1 & (int)YakuGroup1.Pinfu) == 0 && (tmp_ply_flg & RonNaki) == 0)
                fu_wk += 2;

            // 特殊な上がり形 (国士無双など) は符計算をしない
            if ((mf_d[atm_f] & AgariFl) == 0) return;

            // 1. 刻子 (コーツ) の符
            anko_fu_chk();

            // 2. 待ち (アガリ) の符
            agari_fu_chk();

            // 3. 雀頭 (アタマ) の符
            if (mf_d[atm_t] < PkJiha) return; // 数牌の場合は符なし

            // 役牌 (ドラゴン、場風、自風) の場合
            if (((mf_d[atm_t] & 0x3f) >= 0x35) || // 三元牌
                ((mf_d[atm_t] & 0x3f) == kaze0) || // 場風
                ((mf_d[atm_t] & 0x3f) == kaze1))   // 自風
            {
                fu_wk += 2;
                // C++の yakuhi_chk で kaze0 == kaze1 (ダブ東など) の考慮があるが、
                // fu_check の雀頭チェックではダブっても+2符で正しい
            }
        }

        // --- fu_check の依存関数 ---

        // 刻子 (コーツ) の符計算
        private void anko_fu_chk()
        {
            int i, fu, hl, hh;
            for (i = 0; i < mf_d[ank_c]; ++i)
            {
                fu = 2; // 明刻 (ポン)
                hh = (mf_d[ank_t + i] & 0x3f);
                hl = (mf_d[ank_t + i] & 0xf);

                if ((mf_d[ank_t + i] & AkanFl) != 0) fu = 8; // 明槓 (カン)
                if ((mf_d[ank_t + i] & NakiFl) == 0) fu *= 2; // 門前 (暗刻 / 暗槓)

                // 么九牌 (1,9,字牌)
                if ((hh == kaze0) || (hh == kaze1) || (hh >= 0x30) || (hl == 1) || (hl == 9))
                    fu *= 2;

                fu_wk += fu;
            }
        }

        // 待ち (アガリ) の符計算
        private void agari_fu_chk()
        {
            // ペンチャン、カンチャン、タンキ待ちかチェック
            if (agari_chk())
            {
                fu_wk += 2;
            }
        }

        // ペンチャン、カンチャン、タンキ待ちか判定
        private bool agari_chk()
        {
            int i;
            // (tmp_ronh & 0x3F) は和了り牌のタイルID
            if ((tmp_ronh & 0x3F) == mf_d[atm_t]) return true; // 単騎待ち

            // 順子の中での待ちをチェック
            for (i = 0; i < mf_d[jun_c]; ++i)
            {
                // カンチャン (例: 4 で 3-5 を作る)
                if ((mf_d[jun_t + i] + 1) == (tmp_ronh & 0x3F)) return true;

                // ペンチャン (例: 3 で 1-2 を作る)
                if ((mf_d[jun_t + i] == (tmp_ronh & 0x3F)) && ((tmp_ronh & 0xf) == 7)) return true; // 7待ち (8-9持ち)
                if (((mf_d[jun_t + i] + 2) == (tmp_ronh & 0x3F)) && ((tmp_ronh & 0xf) == 3)) return true; // 3待ち (1-2持ち)
            }

            // C++のコードはここで hed_chng (頭変更) を試行している
            // これは平和(Pinfu)判定などで使われるもので、符計算の待ち判定でも
            // 代替の面子構成をチェックする必要がある
            if (hed_chng()) // 頭を入れ替えた別の構成をチェック
            {
                if ((tmp_ronh & 0x3F) == mf_d[atm_t]) return true; // 単騎

                for (i = 0; i < mf_d[jun_c]; ++i)
                {
                    if ((mf_d[jun_t + i] + 1) == (tmp_ronh & 0x3F)) return true; // カンチャン
                    if ((mf_d[jun_t + i] == (tmp_ronh & 0x3F)) && ((tmp_ronh & 0xf) == 7)) return true; // ペンチャン
                    if (((mf_d[jun_t + i] + 2) == (tmp_ronh & 0x3F)) && ((tmp_ronh & 0xf) == 3)) return true; // ペンチャン
                }
            }
            return false;
        }

        // 頭の入れ替えチェック (C++の hed_chng)
        private bool hed_chng()
        {
            int i, atm0, atm1, c;
            if (mf_d[jun_c] < 2) return false;
            if (mf_d[atm_t] > 0x30) return false; // 頭が字牌なら入れ替え不可

            // 例: 頭[44] 順子[123, 123] -> 頭[11] 順子[234, 234] に組み替え
            if ((mf_d[atm_t] & 0xf) >= 4)
            {
                atm1 = atm0 = mf_d[atm_t] - 3; // atm0 = 1
                ++atm1; // atm1 = 2
                for (c = i = 0; i < mf_d[jun_c]; ++i)
                {
                    if (mf_d[jun_t + i] == atm0) ++c;
                }
                if (c >= 2)
                {
                    // 入れ替え可能
                    mf_d[atm_t] = atm0; // 頭を 11 に変更
                    for (c = 2, i = 0; i < mf_d[jun_c]; ++i)
                    {
                        if (mf_d[jun_t + i] == atm0)
                        {
                            mf_d[jun_t + i] = atm1; // 順子を 123 -> 234 に変更
                            if (--c == 0) return true;
                        }
                    }
                }
            }

            // 例: 頭[66] 順子[789, 789] -> 頭[99] 順子[678, 678] に組み替え
            if ((mf_d[atm_t] & 0xf) < 7)
            {
                atm0 = mf_d[atm_t] + 1; // 7
                atm1 = mf_d[atm_t];     // 6

                for (c = i = 0; i < mf_d[jun_c]; ++i)
                {
                    if (mf_d[jun_t + i] == atm0) ++c;
                }
                if (c < 2) return false;

                // 入れ替え可能
                mf_d[atm_t] = atm0 + 2; // 頭を 99 に変更
                for (c = 2, i = 0; i < mf_d[jun_c]; ++i)
                {
                    if (mf_d[jun_t + i] == atm0)
                    {
                        mf_d[jun_t + i] = atm1; // 順子を 789 -> 678 に変更
                        if (--c == 0) return true;
                    }
                }
            }
            return false;
        }

        private void ryanpei_yk()
        {
            if ((tmp_ply_flg & NomNaki) != 0 || (mf_d[atm_f] & AgariFl) == 0) return;

            if (mf_d[jun_c] == 4)
            {
                if ((mf_d[jun_t] == mf_d[jun_t + 1] && mf_d[jun_t + 2] == mf_d[jun_t + 3]))
                {
                    yaku_wk1 &= ~(int)YakuGroup1.Ipei; // Remove Iipeikou
                    yaku_wk3 |= (int)YakuGroup3.Ryanpei;
                    total_han++;
                }
            }
        }

        private void juntya_yk()
        {
            if ((yaku_wk2 & (int)YakuGroup2.Honrou) != 0) return;
            if (juntya_0()) return;
            // Complex logic with anko change skipped for now
        }

        private bool juntya_0()
        {
            if (!tyn_jun_chk()) return false;
            int c = tyn_ank_chk();
            if (c < 0) return false;
            if (c == 0)
            {
                yaku_wk3 |= (int)YakuGroup3.Juntyan;
                total_han++;
            }
            else
            {
                yaku_wk2 |= (int)YakuGroup2.Hontyanta;
                total_han++;
            }
            return true;
        }

        private bool tyn_jun_chk()
        {
            for (int i = 0; i < mf_d[jun_c]; i++)
            {
                if ((mf_d[jun_t + i] & 0xf) != 1 && (mf_d[jun_t + i] & 0xf) != 7)
                {
                    return false;
                }
            }
            return true;
        }

        private int tyn_ank_chk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & 0xf0) == 0x30)
                {
                    c++;
                }
                else if ((mf_d[ank_t + i] & 0xf) != 1 && (mf_d[ank_t + i] & 0xf) != 9) return -1;
            }
            if ((mf_d[atm_t] & 0xf0) == 0x30)
            {
                c++;
            }
            else if ((mf_d[atm_t] & 0xf) != 1 && (mf_d[atm_t] & 0xf) != 9) return -1;
            return c;
        }

        // ===================================================================
        // === ここから C++ (MahjongSub.cpp) の ronchk/men_chk 移植ブロック ===
        // ===================================================================

        // MahjongSub.h
        private const int TMask = 0x0fff;    // ソートに影響しないフラグを除外
        // MahjongSub.h
        private const int SercFl = (1 << 11); // Set End Flasgすでにセットした。
        // MahjongSub.h
        private const int FixFl = (NakiFl | AkanFl); //位置固定
        // MahjongSub.h
        private const int FixsFl = (NakiFl | SercFl | AkanFl);  //サーチ済み総合
        private const int PkMask = 0x30; // 牌の種類(萬筒索/字牌)マスク
        // --- MahjongLogic.cs の から定義されている ---
        private int[] mf_fs = new int[mfd_end];
        private int[] mf_f = new int[mfd_end];
        private int[] mf_fj = new int[mfd_end];
        private int[] mf_fr = new int[mfd_end];
        private int[] mf_fjr = new int[mfd_end];

        private const int atm_f = 0;
        private const int ank_c = 1;
        private const int jun_c = 2;
        private const int atm_t = 3;
        private const int ank_t = 4; // 4, 5, 6, 7
        private const int jun_t = 8; // 8, 9, 10, 11
        // (mf_d の残りは 12, 13, 14)

        /// <summary>
        /// 上がり判定のメインルーチン
        /// C++の ronchk()
        /// </summary>
        private bool ronchk()
        {
            //
            men_chk();

            //
            // C++の ronchk_10() のロジック
            ronchk_10(mf_fs); //
            ronchk_10(mf_f); //
            ronchk_10(mf_fj); //
            ronchk_10(mf_fr); //
            ronchk_10(mf_fjr); //

            //
            if ((agr_flg & AgariFl) != 0) return true;

            //
            if (ronchk_spc()) return true; // 特殊役 (七対子, 国士)

            return false;
        }

        /// <summary>
        /// 上がり形か判定 (4面子1雀頭)
        /// C++の ronchk_10()
        /// </summary>
        private void ronchk_10(int[] mf)
        {
            if (mf[atm_f] == 0) return; // (頭がない)
            if ((mf[ank_c] + mf[jun_c]) != 4) return; // (4面子ない)

            mf[atm_f] |= AgariFl; // (上がりフラグ)
            agr_flg |= AgariFl; //
        }

        /// <summary>
        /// 特殊な上がり形 (国士無双など)
        /// C++の ronchk_spc()
        /// </summary>
        private bool ronchk_spc()
        {
            // C++の spc_men と同じ
            // ※ tmp_wk_v は CheckWin() でソート済み
            int[] wk = (int[])tmp_wk_v.Clone();
            int[] mf = new int[mfd_end];

            if (men7_chk(wk, mf)) //
            {
                agr_flg = AgariFl | 1; // 0x81 ７対
                Array.Copy(mf, mf_fs, mfd_end);
                return true;
            }
            if (men19_chk(wk, mf)) //
            {
                if (mf[atm_f] == 13) //
                {
                    agr_flg = AgariFl | 2; // 0x82 国志無双
                    Array.Copy(mf, mf_fs, mfd_end);
                    return true;
                }
            }
            if (sisan_puta(wk, mf)) //
            {
                agr_flg = AgariFl | 4; // 0x84 Si sanputa
                Array.Copy(mf, mf_fs, mfd_end);
                return true;
            }

            return false;
        }


        /// <summary>
        /// 面子探索のメインルーチン
        /// C++の men_chk()
        /// </summary>
        private void men_chk()
        {
            int i;
            // C++の tmp_wk は CheckWin() ですでにソート＆tmp_wk_vにコピー済み
            //

            agr_flg = 0; //

            // 各探索パターンの結果を初期化
            for (i = 0; i < mfd_end; i++)
            {
                mf_d[i] = mf_fs[i] = mf_f[i] = mf_fj[i] = mf_fr[i] = mf_fjr[i] = 0;
            }

            //....special check.....
            // C++の spc_men は ronchk_spc で別途チェック
            // ここでは通常手を探索

            //.........spectial one check....
            // 1,1,1,4,5,5,6,7,8,8,8,9,9 などの特殊な形
            //
            int[] wk_fs = (int[])tmp_wk_v.Clone();
            anko_sp(wk_fs, mf_fs); //
            atm_srh(wk_fs, mf_fs); //
            suji_chk(wk_fs, mf_fs); //
            sjatm_chk(wk_fs, mf_fs); //

            //.....anko juntu..left to right 
            //
            int[] wk_f = (int[])tmp_wk_v.Clone();
            ankj_sr(wk_f, mf_f); //
            atm_srh(wk_f, mf_f); //
            suji_chk(wk_f, mf_f); //
            sjatm_chk(wk_f, mf_f); //

            //...juntu to anko
            //
            int[] wk_fj = (int[])tmp_wk_v.Clone();
            juna_sr(wk_fj, mf_fj); //
            atm_srh(wk_fj, mf_fj); //
            suji_chk(wk_fj, mf_fj); //
            sjatm_chk(wk_fj, mf_fj); //

            //...anko juntu...right to left
            //
            int[] wk_fr = (int[])tmp_wk_v.Clone();
            ankjr_sr(wk_fr, mf_fr); //
            atm_srh(wk_fr, mf_fr); //
            suji_chk(wk_fr, mf_fr); //
            sjatm_chk(wk_fr, mf_fr); //

            //...juntu anko..
            //
            int[] wk_fjr = (int[])tmp_wk_v.Clone();
            junar_sr(wk_fjr, mf_fjr); //
            atm_srh(wk_fjr, mf_fjr); //
            suji_chk(wk_fjr, mf_fjr); //
            sjatm_chk(wk_fjr, mf_fjr); //
        }

        /// <summary>
        /// 特殊な形のテンパイ探索 (1,1,1,4,5,5,6,7,8,8,8,9,9 など)
        /// C++の anko_sp()
        /// </summary>
        private void anko_sp(int[] wk, int[] mf)
        {
            naki_men(wk, mf); // (鳴き面子を分離)
            for (int i = 0; i < 12; i++)
            {
                if ((wk[i] & FixsFl) == 0) //
                    ank_srh(wk, mf, i); //
            }
            for (int i = 0; i < 12; i++)
            {
                if ((wk[i] & FixsFl) == 0) //
                    jun_srh(wk, mf, i); //
            }
        }

        /// <summary>
        /// 刻子→順子 の順で探索 (左から)
        /// C++の ankj_sr()
        /// </summary>
        private void ankj_sr(int[] wk, int[] mf)
        {
            naki_men(wk, mf); // (鳴き面子を分離)
            for (int i = 0; i < 12; i++)
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if (ank_srh(wk, mf, i)) // (刻子探索)
                        jun_srh(wk, mf, i); // (順子探索)
                }
            }
        }

        /// <summary>
        /// 順子→刻子 の順で探索 (左から)
        /// C++の juna_sr()
        /// </summary>
        private void juna_sr(int[] wk, int[] mf)
        {
            naki_men(wk, mf); //
            for (int i = 0; i < 12; i++)
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if (jun_srh(wk, mf, i)) // (順子探索)
                        ank_srh(wk, mf, i); // (刻子探索)
                }
            }
        }

        /// <summary>
        /// 刻子→順子 の順で探索 (右から)
        /// C++の ankjr_sr()
        /// </summary>
        private void ankjr_sr(int[] wk, int[] mf)
        {
            naki_men(wk, mf); //
            for (int i = 13; i > 1; --i) //
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if (ankr_srh(wk, mf, i)) // (刻子探索)
                        junr_srh(wk, mf, i); // (順子探索)
                }
            }
        }

        /// <summary>
        /// 順子→刻子 の順で探索 (右から)
        /// C++の junar_sr()
        /// </summary>
        private void junar_sr(int[] wk, int[] mf)
        {
            naki_men(wk, mf); //
            for (int i = 13; i > 1; --i) //
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if (junr_srh(wk, mf, i)) // (順子探索)
                        ankr_srh(wk, mf, i); // (刻子探索)
                }
            }
        }

        /// <summary>
        /// 刻子(コーツ)を見つける
        /// C++の ank_srh()
        /// </summary>
        /// <returns>true=見つからない, false=見つけた</returns>
        private bool ank_srh(int[] wk, int[] mf, int ix)
        {
            if (wk[ix] == wk[ix + 1] && wk[ix] == wk[ix + 2]) //
            {
                //On Anko
                mf[ank_t + mf[ank_c]] = wk[ix]; //
                ++mf[ank_c]; //
                wk[ix] |= SercFl; //
                wk[ix + 1] |= SercFl;
                wk[ix + 2] |= SercFl;
                return false;
            }
            return true; //
        }

        /// <summary>
        /// 刻子(コーツ)を見つける (右から)
        /// C++の ankr_srh()
        /// </summary>
        /// <returns>true=見つからない, false=見つけた</returns>
        private bool ankr_srh(int[] wk, int[] mf, int ix)
        {
            if (wk[ix] == wk[ix - 1] && wk[ix] == wk[ix - 2]) //
            {
                //On Anko
                mf[ank_t + mf[ank_c]] = wk[ix]; //
                ++mf[ank_c]; //
                wk[ix] |= SercFl; //
                wk[ix - 1] |= SercFl;
                wk[ix - 2] |= SercFl;
                return false;
            }
            return true; //
        }

        /// <summary>
        /// 順子(ジュンツ)を見つける
        /// C++の jun_srh()
        /// </summary>
        /// <returns>true=見つからない, false=見つけた</returns>
        private bool jun_srh(int[] wk, int[] mf, int ix)
        {
            if ((wk[ix] & PkMask) == PkJiha) return true; //

            int d = wk[ix] + 1;
            for (int i = ix + 1; i < 14; ++i) //
            {
                if ((wk[i] & FixsFl) == 0 && d == wk[i]) //
                {
                    int j = i;
                    ++d;
                    ++i;
                    for (; i < 14; ++i) //
                    {
                        if ((wk[i] & FixsFl) == 0 && d == wk[i]) //
                        {
                            mf[jun_t + mf[jun_c]] = wk[ix]; //
                            ++mf[jun_c]; //
                            wk[ix] |= SercFl; //
                            wk[j] |= SercFl;
                            wk[i] |= SercFl;
                            return false;
                        }
                    }
                }
            }
            return true; //
        }

        /// <summary>
        /// 順子(ジュンツ)を見つける (右から)
        /// C++の junr_srh()
        /// </summary>
        /// <returns>true=見つからない, false=見つけた</returns>
        private bool junr_srh(int[] wk, int[] mf, int ix)
        {
            if ((wk[ix] & PkMask) == PkJiha) return true; //

            int d = wk[ix] - 1;
            for (int i = ix - 1; i > 0; --i) //
            {
                if ((wk[i] & FixsFl) == 0 && d == wk[i]) //
                {
                    int j = i;
                    --d;
                    --i;
                    for (; i >= 0; --i) //
                    {
                        if ((wk[i] & FixsFl) == 0 && d == wk[i]) //
                        {
                            mf[jun_t + mf[jun_c]] = wk[ix] - 2; //
                            ++mf[jun_c]; //
                            wk[ix] |= SercFl; //
                            wk[j] |= SercFl;
                            wk[i] |= SercFl;
                            return false;
                        }
                    }
                }
            }
            return true; //
        }

        /// <summary>
        /// 雀頭(アタマ)を見つける
        /// C++の atm_srh()
        /// </summary>
        private void atm_srh(int[] wk, int[] mf)
        {
            for (int i = 0; i < 13; ++i)
            {
                //
                if ((wk[i] & FixsFl) == 0 && wk[i] == wk[i + 1])
                {
                    mf[atm_t] = wk[i]; //
                    ++mf[atm_f]; //
                    wk[i] |= SercFl; //
                    wk[i + 1] |= SercFl;
                    ++i;
                }
            }
        }

        /// <summary>
        /// 筋(スジ)で頭を見つける (222 34 -> 22 / 234)
        /// C++の sjatm_chk()
        /// </summary>
        private void sjatm_chk(int[] wk, int[] mf)
        {
            if (mf[atm_f] != 0) return; //
            if (mf[ank_c] == 0) return; //
            if ((mf[ank_c] + mf[jun_c]) != 4) return; //

            for (int i = 0; i < 14; ++i)
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if ((wk[i] & PkMask) == PkJiha || i >= 13) return; //
                    int k = i;
                    int d = wk[i++] + 1; //
                    for (; i < 14; ++i)
                    {
                        if (wk[i] == d) // 3,4 の 4 を見つけた
                        {
                            int e = d - 2; // 2
                            ++d; // 5
                            for (int j = 0; j < mf[ank_c]; ++j) //
                            {
                                if (d == mf[ank_t + j]) // 555 があるか (3,4 + 555)
                                {
                                    //
                                    // 3,4, 5,5,5 -> 3,4,5 / 5,5 (頭)
                                    // 刻子(555)を消し、順子(345)と頭(55)を追加
                                    mf[ank_t + j] = 0; //
                                    for (int jj = j + 1; jj < mf[ank_c]; ++jj) //
                                    {
                                        mf[ank_t + jj - 1] = mf[ank_t + jj];
                                    }
                                    mf[ank_t + mf[ank_c] - 1] = 0; // 末尾クリア
                                    --mf[ank_c]; //

                                    mf[jun_t + mf[jun_c]] = d - 2; // 345
                                    ++mf[jun_c]; //

                                    mf[atm_t] = d; // 頭 55
                                    ++mf[atm_f]; //
                                    wk[i] |= SercFl; //
                                    wk[k] |= SercFl; //
                                    return;
                                }
                                else if (e == mf[ank_t + j]) // 222 があるか (222 + 3,4)
                                {
                                    //
                                    // 2,2,2 ,3,4 -> 2,2 (頭) / 2,3,4
                                    mf[ank_t + j] = 0; //
                                    for (int jj = j + 1; jj < mf[ank_c]; ++jj) //
                                    {
                                        mf[ank_t + jj - 1] = mf[ank_t + jj];
                                    }
                                    mf[ank_t + mf[ank_c] - 1] = 0;
                                    --mf[ank_c]; //

                                    mf[jun_t + mf[jun_c]] = e; // 234
                                    ++mf[jun_c]; //

                                    mf[atm_t] = e; // 頭 22
                                    ++mf[atm_f]; //
                                    wk[i] |= SercFl; //
                                    wk[k] |= SercFl; //
                                    return;
                                }
                            }
                            return; //
                        }
                    }
                    return; //
                }
            }
        }

        /// <summary>
        /// 筋(スジ)で頭を見つける (22 345 -> 22 / 345)
        /// C++の suji_chk()
        /// </summary>
        private void suji_chk(int[] wk, int[] mf)
        {
            if (mf[atm_f] != 0 || mf[jun_c] == 0) return; //
            if ((mf[ank_c] + mf[jun_c]) != 4) return; //
            suji_chk_00(wk, mf); //
        }

        /// <summary>
        /// C++の suji_chk_00()
        /// </summary>
        private bool suji_chk_00(int[] wk, int[] mf)
        {
            for (int i = 0; i < 14; ++i)
            {
                if ((wk[i] & FixsFl) == 0) //
                {
                    if ((wk[i] & PkMask) == PkJiha || i >= 13) return false; //
                    int k = i;
                    int d = wk[i++] + 3; //
                    for (; i < 14; ++i)
                    {
                        if (wk[i] == d) // 2 と 5 を見つけた
                        {
                            d -= 3; // d = 2
                            for (int j = 0; j < mf[jun_c]; ++j) //
                            {
                                if (d == mf[jun_t + j]) // 順子(234) があるか
                                {
                                    // 2, 5 + 2,3,4 -> 2,2 (頭) / 3,4,5 (順子)
                                    ++mf[jun_t + j]; // 234 -> 345
                                    mf[atm_t] = d; // 頭 22
                                    ++mf[atm_f]; //
                                    wk[i] |= SercFl; //
                                    wk[k] |= SercFl; //
                                    return true;
                                }
                            }
                            return false; //
                        }
                    }
                    return false; //
                }
            }
            return false; //
        }


        /// <summary>
        /// 七対子 (7ペア)
        /// C++の men7_chk()
        /// </summary>
        private bool men7_chk(int[] wk, int[] mf)
        {
            for (int i = 0; i < 13; i += 2) //
            {
                if (wk[i] == wk[i + 1]) //
                {
                    if (i < 12)
                    {
                        if (wk[i] == wk[i + 2]) return false; // 4枚使い
                    }
                    ++mf[atm_f]; //
                }
                else
                {
                    return false; //
                }
            }
            return true; //
        }

        /// <summary>
        /// 国士無双 (13么九)
        /// C++の men19_chk()
        /// </summary>
        private bool men19_chk(int[] wk, int[] mf)
        {
            mf[atm_f] = 0; //
            for (int i = 0; i < 14; ++i)
            {
                int tile = wk[i] & 0x3F;
                if (tile >= PkJiha || (tile & 0xf) == 1 || (tile & 0xf) == 9) //
                {
                    if (i == 0) ++mf[atm_f]; //
                    else if (wk[i] != wk[i - 1]) ++mf[atm_f]; //
                }
                else
                {
                    if (wk[i] != 0) return false; // 么九牌以外
                }
            }
            return true; //
        }

        /// <summary>
        /// 十三不塔
        /// C++の sisan_puta()
        /// </summary>
        private bool sisan_puta(int[] wk, int[] mf)
        {
            //
            // C++の実装では `ck_no`, `tmp_ply_flg`, `sute_pai_cnt`, `ply_flg` を
            // グローバル変数として参照していますが、C#のクラス内では
            // `this.tmp_ply_flg` などに置き換える必要があります。
            // (この移植では `CheckWin` 経由で `tmp_ply_flg` はセットされています)

            // if (ck_no != 0) return false; // (プレイヤーのみ)
            if ((tmp_ply_flg & RonNaki) != 0) return false; // ロンではない
            // if (sute_pai_cnt[ck_no] != 0) return false; // 配牌時
            // for(int i=0; i<4; ++i) if((ply_flg[i]&NomNaki)!=0) return false; // 誰も鳴いてない

            int pairs = 0;
            for (int i = 0; i < 13; ++i)
            {
                if (wk[i] == wk[i + 1]) pairs++; //

                if ((wk[i] & 0x30) != 0x30) // 数牌
                {
                    if (wk[i] == (wk[i + 1] - 1)) return false; // 1,2
                    if (wk[i] == (wk[i + 1] - 2)) return false; // 1,3
                }
            }
            if (pairs != 1) return false; // ペアが1組だけ
            return true; //
        }

        /// <summary>
        /// 鳴き面子を分離する
        /// C++の naki_men()
        /// </summary>
        private void naki_men(int[] wk, int[] mf)
        {
            for (int i = 0; i < mfd_end; i++) mf[i] = 0; //

            for (int i = 0; i < 12; ++i)
            {
                if ((wk[i] & FixFl) != 0) // (鳴き or カン)
                {
                    if (wk[i] == wk[i + 1])
                    {
                        //Anko on
                        mf[ank_t + mf[ank_c]] = wk[i]; //
                        ++mf[ank_c]; //
                        i += 2; //
                    }
                    else
                    {
                        //Juntu on
                        mf[jun_t + mf[jun_c]] = wk[i]; //
                        ++mf[jun_c]; //
                        i += 2; //
                    }
                }
            }
        }

        // ===================================================================
        // === C++ (MahjongSub.cpp) の ronchk/men_chk 移植ブロック ここまで ===
        // ===================================================================

        private void dora_check()
        {
            // Logic to count dora tiles
        }

        private int jihai_cnt()
        {
            int c = 0;
            for (int i = 0; i < 14; i++)
            {
                if ((tmp_wk_v[i] & 0x30) == 0x30) c++;
            }
            return c;
        }

        private int iqhai_cnt()
        {
            int c = 0;
            for (int i = 0; i < 14; i++)
            {
                if ((tmp_wk_v[i] & 0x30) != 0x30 && ((tmp_wk_v[i] & 0xf) == 1 || (tmp_wk_v[i] & 0xf) == 9)) c++;
            }
            return c;
        }

        private int same_hai_chk()
        {
            int c = 0, d;
            if (tmp_wk_v.Length == 0 || tmp_wk_v[0] == 0) return -1; // Guard clause

            for (int i = 0; i < 14; i++)
            {
                if (tmp_wk_v[i] == 0) continue; // Skip padding

                if ((d = (tmp_wk_v[i] & 0x30)) != 0x30)
                {
                    // Suit tile
                    for (++i; i < 14; ++i)
                    {
                        if (tmp_wk_v[i] == 0) continue;
                        if ((tmp_wk_v[i] & 0x30) == 0x30)
                        {
                            ++c;
                        }
                        else if ((tmp_wk_v[i] & 0x30) != d) return -1; // Different suit
                    }
                    return c; // 0 (Tinitu) or >0 (Honitu)
                }
                ++c; // Honor tile
            }
            return c; // All honor tiles (Tuiisou)
        }


        // Placeholder for constants that were in MahjongSub.h
        private const int AgariFl = (1 << 16);

        // -----------------------------------------------------------------
        // 外部 (DisplayManager) からの呼び出し用メソッド (ここから追加)
        // -----------------------------------------------------------------

        /// <summary>
        /// 外部から役判定を要求するメインメソッド
        /// </summary>
        /// <param name="handTiles">手牌13枚 + 上がり牌1枚 (計14枚) のint配列</param>
        /// <param name="winningTile">上がり牌 (ロンまたはツモ)</param>
        /// <param name="playerWind">自風 (1=東, 2=南, 3=西, 4=北)</param>
        /// <param name="roundWind">場風 (1=東, 2=南, 3=西, 4=北)</param>
        /// <returns>役名のリスト (上がっていない場合は空)</returns>
        /*   public List<string> CheckWin(int[] handTiles, int winningTile, int playerWind, int roundWind, bool isRon)
           {
               // 1. 内部状態をリセット
               yk_work_clr();

               // 2. 外部から受け取った手牌を内部の tmp_wk にコピー
               Array.Clear(tmp_wk, 0, tmp_wk.Length);
               Array.Copy(handTiles, tmp_wk, handTiles.Length);

               // 3. ツモ牌（またはロン牌）を設定
               tmp_ronh = winningTile;

               // 4. 風を設定 (C++の kaze_pai_set の簡易実装)
               kaze0 = 0x30 | roundWind; // 場風
               kaze1 = 0x30 | playerWind; // 自風

               // 5. プレイヤー状態を設定 (仮: 門前ツモとする)
               // ★注意: リーチ、鳴き、ロン/ツモのフラグは別途 DisplayManager から渡す必要があります
               tmp_ply_flg = 0; // 門前・ツモ・リーチなし

               // 6. 手牌をソート (ronchk の前提条件)
               Array.Sort(tmp_wk, 0, 14);

               // tmp_wk_v にもコピー (YakuCheckで使われるため)
               Array.Clear(tmp_wk_v, 0, tmp_wk_v.Length);
               Array.Copy(tmp_wk, tmp_wk_v, 14);

               // 7. 役判定の実行
               // ★★★注意★★★
               // MahjongLogic.cs の ronchk() が正しく実装されていないと、ここは常に false になります。
               bool isAgari = ronchk();

               if (isAgari)
               {
                   // 役チェックを実行
                   YakuCheck();

                   // 役名を取得
                   return GetYakuNames();
               }

               return new List<string>(); // 上がりではない
           }*/
        /// <summary>
        /// 外部から役判定を要求するメインメソッド
        /// </summary>
        // ★ 修正: bool isRon パラメータを追加
        public List<string> CheckWin(int[] handTiles, int winningTile, int playerWind, int roundWind, bool isRon)
        {
            // 1. 内部状態をリセット
            yk_work_clr();
            ck_no = 0; // プレイヤー番号を0と仮定

            // 2. 外部から受け取った手牌を内部の tmp_wk にコピー
            Array.Clear(tmp_wk, 0, tmp_wk.Length);
            Array.Copy(handTiles, tmp_wk, handTiles.Length);

            // 3. ツモ牌（またはロン牌）を設定
            tmp_ronh = winningTile;

            // 4. 風を設定 (C++の kaze_pai_set の簡易実装)
            kaze0 = 0x30 | roundWind; // 場風
            kaze1 = 0x30 | playerWind; // 自風

            // 5. プレイヤー状態を設定
            // ★ 修正: isRon に応じて tmp_ply_flg を設定
            if (isRon)
            {
                tmp_ply_flg = RonNaki; // Ron
            }
            else
            {
                tmp_ply_flg = 0; // Tsumo
            }
            // (NomNaki (鳴き) flag は0のまま (門前と仮定))

            // 6. 手牌をソート (ronchk の前提条件)
            Array.Sort(tmp_wk, 0, 14);

            // tmp_wk_v にもコピー (YakuCheckで使われるため)
            Array.Clear(tmp_wk_v, 0, tmp_wk_v.Length);
            Array.Copy(tmp_wk, tmp_wk_v, 14);

            // 7. 役判定の実行
            bool isAgari = ronchk(); //

            if (isAgari)
            {
                if (YakuCheck()) // YakuCheck()が翻計算まで行う
                {
                    // 役名リストを返す
                    return GetYakuNames();
                }
            }

            return new List<string>(); // 上がりではない
        }

        /// <summary>
        /// 判定結果（役名＋翻数）を取得するヘルパー
        /// </summary>
        public List<string> GetYakuNames()
        {
            List<string> names = new List<string>();
            int[] sco_tbl;

            // 役満
            sco_tbl = yaku_wk0_han_tb1; //
            for (int i = 0; i < yaku_wk0_tbl.Length; i++)
            {
                if ((yaku_wk0 & (1 << i)) != 0)
                {
                    string hanStr = (sco_tbl[i] & 0x3) > 1 ? "ダブル役満" : "役満";
                    names.Add($"{yaku_wk0_tbl[i]} ({hanStr})");
                }
            }

            if (names.Count > 0) return names; // 役満の場合は他の役を表示しない

            // 3-6翻
            sco_tbl = yaku_wk3_han_tb1; //
            if ((tmp_ply_flg & NomNaki) != 0) sco_tbl = yaku_wk3_han_tb2; //
            for (int i = 0; i < yaku_wk3_tbl.Length; i++)
            {
                if ((yaku_wk3 & (1 << i)) != 0)
                {
                    if (sco_tbl[i] > 0) // 二盃口(鳴き)などは0翻
                        names.Add($"{yaku_wk3_tbl[i]} ({sco_tbl[i]}翻)");
                }
            }
            // 2翻
            sco_tbl = yaku_wk2_han_tb1; //
            if ((tmp_ply_flg & NomNaki) != 0) sco_tbl = yaku_wk2_han_tb2; //
            for (int i = 0; i < yaku_wk2_tbl.Length; i++)
            {
                if ((yaku_wk2 & (1 << i)) != 0)
                {
                    names.Add($"{yaku_wk2_tbl[i]} ({sco_tbl[i]}翻)");
                }
            }
            // 1翻
            sco_tbl = yaku_wk1_han_tb1; //
            for (int i = 0; i < yaku_wk1_tbl.Length; i++)
            {
                if ((yaku_wk1 & (1 << i)) != 0)
                {
                    names.Add($"{yaku_wk1_tbl[i]} ({sco_tbl[i]}翻)");
                }
            }
            // 役牌
            if (yakuh_wk > 0)
            {
                names.Add($"{yaku_wk_a_tbl[0]} ({yakuh_wk}翻)"); // "役牌 (2翻)"
            }
            // ドラ
            if (dora_wk > 0)
            {
                names.Add($"{yaku_wk_a_tbl[1]} ({dora_wk}翻)"); // "ドラ (3翻)"
            }

            return names;
        }
        
        /// <summary>
        /// 符、翻、点数を計算して文字列として返す
        /// C++の get_score をベースに
        /// </summary>
        /// <returns>例: "8翻 30符 満貫 12000点"</returns>
        public string GetScoreSummary()
        {
            // C++ 親(Oya)かどうか
            bool isOya = (kaze0 == kaze1); 

            int[] cul_score = new int[4]; //
            string yakuName = "";
            int han_for_calc = total_han;
            int fu_for_calc = fu_wk;

            // 役満 or 13翻以上
            if ((ykmn_cnt != 0) || han_for_calc >= 13)
            {
                if (ykmn_cnt == 0) ykmn_cnt = 1; // 数え役満
                int base_score = isOya ? oya_yakuten : ko_yakuten; //
                cul_score[0] = base_score * ykmn_cnt; // ロン
                cul_score[1] = base_score * ykmn_cnt; // ツモ
                yakuName = (ykmn_cnt > 1) ? $"{ykmn_cnt}倍役満 " : "役満 ";
            }
            // 七対子
            else if ((yaku_wk2 & (int)YakuGroup2.Titoitu) != 0)
            {
                fu_for_calc = 25; // 25符固定
                int[] sco_tbl = isOya ? ti_scr_tbl_oya : ti_scr_tbl_ko; //
                int ix = (han_for_calc - 2) * 4;
                if (ix < 0) ix = 0;
                if (ix >= sco_tbl.Length) ix = sco_tbl.Length - 4; // 4翻まで
                for (int i = 0; i < 4; ++i) cul_score[i] = sco_tbl[ix + i] * 100;
            }
            else // 通常役
            {
                // 符を丸める
                int fuc_index = (fu_for_calc + 9) / 10; // 20符->2, 30符->3
                
                // C++ (fu_check) / (get_score)
                // 平和ツモ(20符)以外は30符に切り上げ
                if (fuc_index < 3 && fu_for_calc > 20) fuc_index = 3; // 22符 -> 30符
                if (fu_for_calc == 20 && (yaku_wk1 & (int)YakuGroup1.Pinfu) == 0 && (tmp_ply_flg & RonNaki) == 0) fuc_index = 3; // 平和なしツモ(20符) -> 30符
                if (fuc_index < 2) fuc_index = 2; // 最小 (平和ロンなど)

                // 満貫切り上げ
                if (han_for_calc == 5) { yakuName = "満貫 "; }
                else if (han_for_calc >= 6 && han_for_calc <= 7) { yakuName = "跳満 "; han_for_calc = 6; }
                else if (han_for_calc >= 8 && han_for_calc <= 10) { yakuName = "倍満 "; han_for_calc = 8; }
                else if (han_for_calc >= 11 && han_for_calc <= 12) { yakuName = "三倍満 "; han_for_calc = 11; }
                else if (han_for_calc == 4 && fuc_index >= 4) { yakuName = "満貫 "; han_for_calc = 5; } // 4翻40符
                else if (han_for_calc == 3 && fuc_index >= 7) { yakuName = "満貫 "; han_for_calc = 5; } // 3翻70符
                
                int[] sco_tbl;
                int ix;
                
                // 5翻以上
                if(han_for_calc >= 5)
                {
                    sco_tbl = isOya ? score_tbl_2 : score_tbl_0; //
                    ix = (han_for_calc - 5) * 4;
                    if (ix >= sco_tbl.Length) ix = sco_tbl.Length - 4; // 12翻まで
                }
                else // 4翻以下
                {
                    sco_tbl = isOya ? score_tbl_1 : score_tbl; //
                    if (fuc_index > 8) fuc_index = 8; // 80符まで
                    ix = (fuc_index - 2) * 4 + (han_for_calc * 4 * 7);
                }
                
                for (int i = 0; i < 4; ++i) cul_score[i] = sco_tbl[ix + i] * 100;
            }
            
            // C++ ロン or ツモ
            int finalScore = (tmp_ply_flg & RonNaki) != 0 ? cul_score[0] : cul_score[1];
            
            if (ykmn_cnt > 0)
            {
                return $"{yakuName}{finalScore}点";
            }
            else
            {
                // 符を丸める (七対子は25符、他は切り上げ)
                int display_fu = (fu_wk == 25) ? 25 : ((fu_wk + 9) / 10 * 10);
                if (display_fu < 30 && fu_wk > 20) display_fu = 30; // 22符 -> 30符
                if (display_fu == 20 && (yaku_wk1 & (int)YakuGroup1.Pinfu) == 0 && (tmp_ply_flg & RonNaki) == 0) display_fu = 30; // 平和なしツモ

                return $"{total_han}翻 {display_fu}符 {yakuName}{finalScore}点";
            }
        }
        /// <summary>
        /// 判定結果（役名）を取得するヘルパー
        /// </summary>
      /*  public List<string> GetYakuNames()
        {
            List<string> names = new List<string>();

            // 役満
            for (int i = 0; i < yaku_wk0_tbl.Length; i++)
            {
                if ((yaku_wk0 & (1 << i)) != 0)
                {
                    names.Add(yaku_wk0_tbl[i]);
                }
            }

            if (names.Count > 0) return names; // 役満の場合は他の役を表示しない

            // 3-6翻
            for (int i = 0; i < yaku_wk3_tbl.Length; i++)
            {
                if ((yaku_wk3 & (1 << i)) != 0)
                {
                    names.Add(yaku_wk3_tbl[i]);
                }
            }
            // 2翻
            for (int i = 0; i < yaku_wk2_tbl.Length; i++)
            {
                if ((yaku_wk2 & (1 << i)) != 0)
                {
                    names.Add(yaku_wk2_tbl[i]);
                }
            }
            // 1翻
            for (int i = 0; i < yaku_wk1_tbl.Length; i++)
            {
                if ((yaku_wk1 & (1 << i)) != 0)
                {
                    names.Add(yaku_wk1_tbl[i]);
                }
            }
            // 役牌
            if (yakuh_wk > 0)
            {
                names.Add(yaku_wk_a_tbl[0] + " " + yakuh_wk); // "役牌 2" など
            }
            // ドラ
            if (dora_wk > 0)
            {
                names.Add(yaku_wk_a_tbl[1] + " " + dora_wk); // "ドラ 3" など
            }

            return names;
        }*/
    }
}