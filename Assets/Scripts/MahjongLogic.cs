
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
        private int kaze0, kaze1; // Wind tiles
        private int[] tmp_wk = new int[14];
        private int[] tmp_wk_v = new int[14];
        private int[] mf_d = new int[mfd_end];
        private int agr_flg;
        private int tmp_ply_flg;
        private int tmp_ronh;
        private const int mfd_end = 15; // Placeholder, adjust based on actual definition


        public bool YakuCheck()
        {
            yk_work_clr();
            kaze_pai_set();

            // This part of the logic needs the result of the hand analysis (ronchk)
            // For now, I will assume the hand analysis has been done and the results are in mf_d, mf_fs, etc.
            // The hand analysis part (ronchk and its helpers) is very complex and will be ported next.

            // A placeholder for the complex hand analysis result
            int a_f = 0; // (mf_fs[atm_f] | mf_f[atm_f] | mf_fj[atm_f] | mf_fr[atm_f] | mf_fjr[atm_f]);

            if ((a_f & AgariFl) == 0)
            {
                // Special hands (Kokushi, Chitoitsu, etc.)
                if ((agr_flg & 2) != 0)
                {
                    kokusi_yk();
                }
                /*else if ((agr_flg & 4) != 0)
                {
                    total_han++;
                    yaku_wk0 |= (int)Yaku.ShisanPutou;
                }*/
                else
                {
                    toitu7_yk();
                    honrou_yk_1();
                    hontin_yk();
                    tanyao_yk();
                    agrpt_yk();
                    tuuiso_yk();
                }

                // if ((yaku_wk0 & (int)Yaku.ShisanPutou) == 0)
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
            if (total_han == 0) return false;

            if (yaku_wk0 == 0)
            {
                dora_check();
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
            // It depends on the full hand analysis logic.
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
                if ((tmp_ronh & 0xFFFF) == tmp_wk[i]) count++;
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
            // Simplified version of agari pattern check
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
        private void daisan_yk() { }
        private void suusi_yk() { }
        private void tinrou_yk() { }
        private void ryuiso_yk() { }
        private void tyuren_yk() { }
        private void suuank_yk() { }
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
            // if (hed_chng() && rya_chk_0()) return true; // hed_chng is complex, handle later
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
            // hed_chng logic is complex, skipping for now
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
            // hed_chng logic is complex, skipping for now
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
        private void sanankou_yk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & NakiFl) == 0) // Check if concealed
                {
                    int a = mf_d[ank_t + i] & 0x3f;
                    if ((tmp_ply_flg & RonNaki) != 0 && a == (tmp_ronh & 0xFFFF))
                    {
                        // Complex logic for ron on a triplet, simplified for now
                    }
                    else
                    {
                        c++;
                    }
                }
            }
            if (c == 3)
            {
                yaku_wk2 |= (int)YakuGroup2.Sanankou;
                total_han++;
            }
        }

        private void sankantu_yk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & AkanFl) != 0) c++;
            }
            if (c == 3)
            {
                yaku_wk2 |= (int)YakuGroup2.Sankantu;
                total_han++;
            }
        }

        private void syo3gen_yk()
        {
            int c = 0;
            for (int i = 0; i < mf_d[ank_c]; i++)
            {
                if ((mf_d[ank_t + i] & 0x3F) >= 0x35) c++;
            }
            if (c == 2 && mf_d[atm_t] >= 0x35)
            {
                yaku_wk2 |= (int)YakuGroup2.Syo3gen;
                total_han++;
            }
        }

        private void honroutou_yk()
        {
            if ((mf_d[ank_c] == 4) && (jihai_cnt() + iqhai_cnt()) == 14)
            {
                yaku_wk2 |= (int)YakuGroup2.Honrou;
                total_han++;
            }
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


        private int[] mf_fs = new int[mfd_end];
        private int[] mf_f = new int[mfd_end];
        private int[] mf_fj = new int[mfd_end];
        private int[] mf_fr = new int[mfd_end];
        private int[] mf_fjr = new int[mfd_end];

        private const int atm_f = 0;
        private const int ank_c = 1;
        private const int jun_c = 2;
        private const int atm_t = 3;
        private const int ank_t = 4;
        private const int jun_t = 8;

        private bool ronchk()
        {
            for (int i = 0; i < mfd_end; i++)
            {
                mf_f[i] = mf_fs[i] = mf_fj[i] = mf_fr[i] = mf_fjr[i] = 0;
            }

            men_chk();

            if (tmen_chk())
            {
                return true;
            }

            return ronchk_spc();
        }

        private void men_chk()
        {
            // Search for normal winning hands (4 melds, 1 pair)
            ankj_sr(tmp_wk, mf_f); // Search for triplets first
            juna_sr(tmp_wk, mf_f); // Then search for sequences
        }

        private bool tmen_chk()
        {
            // Check if a valid hand was found
            if ((mf_f[atm_f] & AgariFl) != 0) return true;
            if ((mf_fs[atm_f] & AgariFl) != 0) return true;
            if ((mf_fj[atm_f] & AgariFl) != 0) return true;
            if ((mf_fr[atm_f] & AgariFl) != 0) return true;
            if ((mf_fjr[atm_f] & AgariFl) != 0) return true;
            return false;
        }

        private bool ronchk_spc()
        {
            // Check for special hands
            if (men7_chk(tmp_wk, mf_fs)) // Check for 7 pairs (Chii Toitsu)
            {
                agr_flg = 1;
                return true;
            }
            if (men19_chk(tmp_wk, mf_fs)) // Check for 13 Orphans (Kokushi Musou)
            {
                agr_flg = 2;
                return true;
            }
            if (sisan_puta(tmp_wk, mf_fs)) // Check for 13 unrelated tiles
            {
                agr_flg = 4;
                return true;
            }
            return false;
        }

        private void ankj_sr(int[] wk, int[] mf)
        {
            if (mf[ank_c] >= 4)
            {
                // Found 4 melds, check for pair
                atm_srh(wk, mf);
                return;
            }

            if (!ank_srh(wk, mf, 0))
            {
                // No more triplets found, check for pair
                atm_srh(wk, mf);
            }
        }

        private bool ank_srh(int[] wk, int[] mf, int ix)
        {
            bool found = false;
            for (int i = ix; i < 12; i++)
            {
                if ((wk[i] & 0xFFF) != 0xFFF) // Check if not already part of a meld
                {
                    if (wk[i] == wk[i + 1] && wk[i] == wk[i + 2])
                    {
                        found = true;
                        int[] next_wk = (int[])wk.Clone();
                        int[] next_mf = (int[])mf.Clone();

                        next_mf[ank_t + next_mf[ank_c]] = wk[i];
                        next_mf[ank_c]++;

                        next_wk[i] = next_wk[i + 1] = next_wk[i + 2] = 0xFFF; // Mark as used

                        // Recurse
                        ankj_sr(next_wk, next_mf);
                        juna_sr(next_wk, next_mf);
                    }
                }
            }
            return found;
        }

        private void atm_srh(int[] wk, int[] mf)
        {
            for (int i = 0; i < 13; i++)
            {
                if ((wk[i] & 0xFFF) != 0xFFF)
                {
                    if (wk[i] == wk[i + 1])
                    {
                        mf[atm_t] = wk[i];
                        mf[atm_f] = AgariFl; // Found a pair, this is a winning hand
                        // Here we would save the complete hand combination
                        // For now, just marking it as a win.
                        return;
                    }
                }
            }
        }
        private void juna_sr(int[] wk, int[] mf)
        {
            if ((mf[ank_c] + mf[jun_c]) >= 4)
            {
                atm_srh(wk, mf);
                return;
            }

            if (!jun_srh(wk, mf, 0))
            {
                atm_srh(wk, mf);
            }
        }

        private bool jun_srh(int[] wk, int[] mf, int ix)
        {
            bool found = false;
            for (int i = ix; i < 12; i++)
            {
                if ((wk[i] & 0xFFF) != 0xFFF)
                {
                    int p1 = wk[i];
                    int p2 = -1, p2_ix = -1;
                    int p3 = -1, p3_ix = -1;

                    // Find next tile for sequence
                    for (int j = i + 1; j < 14; j++)
                    {
                        if ((wk[j] & 0xFFF) != 0xFFF && wk[j] > p1)
                        {
                            if (wk[j] == p1 + 1)
                            {
                                p2 = wk[j];
                                p2_ix = j;
                                break;
                            }
                        }
                    }

                    if (p2 != -1)
                    {
                        // Find third tile for sequence
                        for (int k = p2_ix + 1; k < 14; k++)
                        {
                            if ((wk[k] & 0xFFF) != 0xFFF && wk[k] > p2)
                            {
                                if (wk[k] == p2 + 1)
                                {
                                    p3 = wk[k];
                                    p3_ix = k;
                                    break;
                                }
                            }
                        }
                    }

                    if (p3 != -1)
                    {
                        found = true;
                        int[] next_wk = (int[])wk.Clone();
                        int[] next_mf = (int[])mf.Clone();

                        next_mf[jun_t + next_mf[jun_c]] = p1;
                        next_mf[jun_c]++;

                        next_wk[i] = next_wk[p2_ix] = next_wk[p3_ix] = 0xFFF;

                        // Recurse
                        juna_sr(next_wk, next_mf);
                    }
                }
            }
            return found;
        }
        private bool men7_chk(int[] wk, int[] mf)
        {
            int i, j, c;
            for (i = 0; i < 13; i++)
            {
                if (wk[i] == wk[i + 1])
                {
                    i++;
                }
                else
                {
                    return false;
                }
            }

            // It's 7 pairs, now check if it's also Ryanpei-kou
            c = 0;
            for (i = 0; i < 6; i++)
            {
                for (j = i + 1; j < 7; j++)
                {
                    if ((wk[i * 2] + 1) == wk[j * 2])
                    {
                        if ((wk[i * 2] + 2) == wk[j * 2])
                        {
                            c++;
                        }
                    }
                }
            }
            if (c >= 2) mf[jun_c] = 2; // Flag for Ryanpeikou potential

            mf[atm_f] = AgariFl;
            return true;
        }

        private bool men19_chk(int[] wk, int[] mf)
        {
            int[] kokusi_tbl = { 0x01, 0x09, 0x11, 0x19, 0x21, 0x29, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37 };
            int i, j, c = 0;
            bool head = false;

            for (i = 0; i < 13; i++)
            {
                for (j = 0; j < 14; j++)
                {
                    if (kokusi_tbl[i] == (wk[j] & 0x3F))
                    {
                        c |= (1 << i);
                        break;
                    }
                }
            }

            if (c != 0x1FFF) return false;

            // Check for the pair
            for (i = 0; i < 13; i++)
            {
                if (wk[i] == wk[i + 1])
                {
                    head = true;
                    break;
                }
            }

            if (head)
            {
                mf[atm_f] = AgariFl;
                return true;
            }

            return false;
        }
        private bool sisan_puta(int[] wk, int[] mf) { /* Porting this check */ return false; }

        private void dora_check()
        {
            // Logic to count dora tiles
        }

        private int jihai_cnt()
        {
            int c = 0;
            for (int i = 0; i < 14; i++)
            {
                if ((tmp_wk[i] & 0x30) == 0x30) c++;
            }
            return c;
        }

        private int iqhai_cnt()
        {
            int c = 0;
            for (int i = 0; i < 14; i++)
            {
                if ((tmp_wk[i] & 0x30) != 0x30 && ((tmp_wk[i] & 0xf) == 1 || (tmp_wk[i] & 0xf) == 9)) c++;
            }
            return c;
        }

        private int same_hai_chk()
        {
            int c = 0, d;
            for (int i = 0; i < 14; i++)
            {
                if ((d = (tmp_wk[i] & 0x30)) != 0x30)
                {
                    for (++i; i < 14; ++i)
                    {
                        if ((tmp_wk[i] & 0x30) == 0x30)
                        {
                            ++c;
                        }
                        else if ((tmp_wk[i] & 0x30) != d) return -1;
                    }
                    return c;
                }
                ++c;
            }
            return c;
        }


        // Placeholder for constants that were in MahjongSub.h
        private const int AgariFl = (1 << 16);
    }
}
