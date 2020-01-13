using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Entities
{
    internal static class DoubleMetaphone
    {
        private static readonly string[] GN_KN_PN_WR_PS = new[] { "GN", "KN", "PN", "WR", "PS" };
        private static readonly string[] ACH = new[] { "ACH" };
        private static readonly string[] BACHER_MACHER = new[] { "BACHER", "MACHER" };
        private static readonly string[] CAESAR = new[] { "CAESAR" };
        private static readonly string[] CHIA = new[] { "CHIA" };
        private static readonly string[] CH = new[] { "CH" };
        private static readonly string[] CHAE = new[] { "CHAE" };
        private static readonly string[] HARAC_HARIS_HOR_HYM_HIA_HEM = new[] { "HARAC", "HARIS", "HOR", "HYM", "HIA", "HEM" };
        private static readonly string[] CHORE = new[] { "CHORE" };
        private static readonly string[] SCH = new[] { "SCH" };
        private static readonly string[] VAN__VON__SCH = new[] { "VAN ", "VON ", "SCH" };
        private static readonly string[] ORCHES_ARCHIT_ORCHID = new[] { "ORCHES", "ARCHIT", "ORCHID" };
        private static readonly string[] T_S = new[] { "T", "S" };
        private static readonly string[] A_O = new[] { "A", "O" };
        private static readonly string[] A_O_U_E = new[] { "A", "O", "U", "E" };
        private static readonly string[] L_R_N_M_B_H_F_V_W__ = new[] { "L", "R", "N", "M", "B", "H", "F", "V", "W", " " };
        private static readonly string[] MC = new[] { "MC" };
        private static readonly string[] CZ = new[] { "CZ" };
        private static readonly string[] WICZ = new[] { "WICZ" };
        private static readonly string[] CIA = new[] { "CIA" };
        private static readonly string[] CC = new[] { "CC" };
        private static readonly string[] I_E_H = new[] { "I", "E", "H" };
        private static readonly string[] HU = new[] { "HU" };
        private static readonly string[] UCCEE_UCCES = new[] { "UCCEE", "UCCES" };
        private static readonly string[] CK_CG_CQ = new[] { "CK", "CG", "CQ" };
        private static readonly string[] CI_CE_CY = new[] { "CI", "CE", "CY" };
        private static readonly string[] CIO_CIE_CIA = new[] { "CIO", "CIE", "CIA" };
        private static readonly string[] _C__Q__G = new[] { " C", " Q", " G" };
        private static readonly string[] C_K_Q = new[] { "C", "K", "Q" };
        private static readonly string[] CE_CI = new[] { "CE", "CI" };
        private static readonly string[] DG = new[] { "DG" };
        private static readonly string[] I_E_Y = new[] { "I", "E", "Y" };
        private static readonly string[] DT_DD = new[] { "DT", "DD" };
        private static readonly string[] B_H_D = new[] { "B", "H", "D" };
        private static readonly string[] B_H = new[] { "B", "H" };
        private static readonly string[] C_G_L_R_T = new[] { "C", "G", "L", "R", "T" };
        private static readonly string[] EY = new[] { "EY" };
        private static readonly string[] LI = new[] { "LI" };
        private static readonly string[] Y_ES_EP_EB_EL_EY_IB_IL_IN_IE_EI_ER = new[] { "Y", "ES", "EP", "EB", "EL", "EY", "IB", "IL", "IN", "IE", "EI", "ER" };
        private static readonly string[] Y_ER = new[] { "Y", "ER" };
        private static readonly string[] DANGER_RANGER_MANGER = new[] { "DANGER", "RANGER", "MANGER" };
        private static readonly string[] E_I = new[] { "E", "I" };
        private static readonly string[] RGY_OGY = new[] { "RGY", "OGY" };
        private static readonly string[] E_I_Y = new[] { "E", "I", "Y" };
        private static readonly string[] AGGI_OGGI = new[] { "AGGI", "OGGI" };
        private static readonly string[] ET = new[] { "ET" };
        private static readonly string[] JOSE = new[] { "JOSE" };
        private static readonly string[] SAN_ = new[] { "SAN " };
        private static readonly string[] L_T_K_S_N_M_B_Z = new[] { "L", "T", "K", "S", "N", "M", "B", "Z" };
        private static readonly string[] S_K_L = new[] { "S", "K", "L" };
        private static readonly string[] ILLO_ILLA_ALLE = new[] { "ILLO", "ILLA", "ALLE" };
        private static readonly string[] AS_OS = new[] { "AS", "OS" };
        private static readonly string[] ALLE = new[] { "ALLE" };
        private static readonly string[] UMB = new[] { "UMB" };
        private static readonly string[] P_B = new[] { "P", "B" };
        private static readonly string[] IE = new[] { "IE" };
        private static readonly string[] IER = new[] { "IER" };
        private static readonly string[] ER = new[] { "ER" };
        private static readonly string[] ME_MA = new[] { "ME", "MA" };
        private static readonly string[] ISL_YSL = new[] { "ISL", "YSL" };
        private static readonly string[] SUGAR = new[] { "SUGAR" };
        private static readonly string[] SH = new[] { "SH" };
        private static readonly string[] HEIM_HOEK_HOLM_HOLZ = new[] { "HEIM", "HOEK", "HOLM", "HOLZ" };
        private static readonly string[] SIO_SIA = new[] { "SIO", "SIA" };
        private static readonly string[] SIAN = new[] { "SIAN" };
        private static readonly string[] M_N_L_W = new[] { "M", "N", "L", "W" };
        private static readonly string[] SC = new[] { "SC" };
        private static readonly string[] OO_ER_EN_UY_ED_EM = new[] { "OO", "ER", "EN", "UY", "ED", "EM" };
        private static readonly string[] ER_EN = new[] { "ER", "EN" };
        private static readonly string[] AI_OI = new[] { "AI", "OI" };
        private static readonly string[] S_Z = new[] { "S", "Z" };
        private static readonly string[] TION = new[] { "TION" };
        private static readonly string[] TIA_TCH = new[] { "TIA", "TCH" };
        private static readonly string[] TH_TTH = new[] { "TH", "TTH" };
        private static readonly string[] OM_AM = new[] { "OM", "AM" };
        private static readonly string[] T_D = new[] { "T", "D" };
        private static readonly string[] WR = new[] { "WR" };
        private static readonly string[] WH = new[] { "WH" };
        private static readonly string[] EWSKI_EWSKY_OWSKI_OWSKY = new[] { "EWSKI", "EWSKY", "OWSKI", "OWSKY" };
        private static readonly string[] WICZ_WITZ = new[] { "WICZ", "WITZ" };
        private static readonly string[] IAU_EAU = new[] { "IAU", "EAU" };
        private static readonly string[] AU_OU = new[] { "AU", "OU" };
        private static readonly string[] C_X = new[] { "C", "X" };
        private static readonly string[] ZO_ZI_ZA = new[] { "ZO", "ZI", "ZA" };

        private static readonly string[] EmptyKeys = new string[0];
        private static readonly int MaxLength = 4;

        private static readonly Regex regex = new Regex(@"\w(?<!\d)[\w'-]*", RegexOptions.Compiled);

        private static void Add(string main, ref StringBuilder sbPrimary, ref StringBuilder sbSecondary)
        {
            sbPrimary.Append(main);
            sbSecondary.Append(main);
        }

        private static void Add(string main, string alternate, ref StringBuilder sbPrimary, ref StringBuilder sbSecondary, ref bool hasAlternate)
        {
            sbPrimary.Append(main);
            if (alternate.Length > 0)
            {
                hasAlternate = true;
                if (!alternate.Equals(" "))
                    sbSecondary.Append(alternate);
            }
            else
            {
                if (main.Length > 0 && !main.Equals(" "))
                    sbSecondary.Append(main);
            }
        }

        private static bool Match(string stringRenamed, int pos, string[] strings)
        {
            if (0 <= pos && pos < stringRenamed.Length)
            {
                for (int n = strings.Length - 1; n >= 0; n--)
                {
                    if (string.Compare(stringRenamed, pos, strings[n], 0, strings[n].Length) == 0)
                        return true;
                }
            }
            return false;
        }

        private static bool Match(string stringRenamed, int pos, char c)
        {
            return (0 <= pos && pos < stringRenamed.Length) && stringRenamed[pos] == c;
        }

        private static bool IsSlavoGermanic(string stringRenamed)
        {
            return (stringRenamed.IndexOf('W') >= 0) || (stringRenamed.IndexOf('K') >= 0) || (stringRenamed.IndexOf("CZ", StringComparison.Ordinal) >= 0) || (stringRenamed.IndexOf("WITZ", StringComparison.Ordinal) >= 0);
        }

        private static bool IsVowel(string stringRenamed, int pos)
        {
            if (pos < 0 || stringRenamed.Length <= pos)
                return false;

            char c = stringRenamed[pos];
            return c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
        }

        private static string[] BuildKeys(string word)
        {
            if (string.IsNullOrEmpty(word))
                return EmptyKeys;

            word = word.ToUpper();

            StringBuilder sbPrimary = new StringBuilder(word.Length);
            StringBuilder sbSecondary = new StringBuilder(word.Length);
            bool hasAlternate = false;
            int length = word.Length;
            int last = length - 1;
            bool isSlavoGermanic = IsSlavoGermanic(word);
            int n = 0;

            if (Match(word, 0, GN_KN_PN_WR_PS))
                n++;

            if (Match(word, 0, 'X'))
            {
                Add("S", ref sbPrimary, ref sbSecondary);
                n++;
            }

            while (n < length && (MaxLength < 0 || (sbPrimary.Length < MaxLength && sbSecondary.Length < MaxLength)))
            {
                switch (word[n])
                {

                    case 'A':
                    case 'E':
                    case 'I':
                    case 'O':
                    case 'U':
                    case 'Y':
                        if (n == 0)
                            Add("A", ref sbPrimary, ref sbSecondary);
                        n++;
                        break;
                    case 'B':
                        Add("P", ref sbPrimary, ref sbSecondary);
                        n += (Match(word, n + 1, 'B') ? 2 : 1);
                        break;
                    case 'Ç':
                        Add("S", ref sbPrimary, ref sbSecondary);
                        n++;
                        break;
                    case 'C':
                        if ((n > 1) && !IsVowel(word, n - 2) && Match(word, n - 1, ACH) && !Match(word, n + 2, 'I') && (!Match(word, n + 2, 'E') || Match(word, n - 2, BACHER_MACHER)))
                        {
                            Add("K", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if ((n == 0) && Match(word, n, CAESAR))
                        {
                            Add("S", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, CHIA))
                        {
                            Add("K", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, CH))
                        {
                            if ((n > 0) && Match(word, n, CHAE))
                            {
                                Add("K", "X", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                n += 2;
                                break;
                            }

                            if ((n == 0) && Match(word, n + 1, HARAC_HARIS_HOR_HYM_HIA_HEM) && !Match(word, 0, CHORE))
                            {
                                Add("K", ref sbPrimary, ref sbSecondary);
                                n += 2;
                                break;
                            }

                            if (Match(word, 0, VAN__VON__SCH) || Match(word, n - 2, ORCHES_ARCHIT_ORCHID) || Match(word, n + 2, T_S) || (((n == 0) || Match(word, n - 1, A_O_U_E)) && Match(word, n + 2, L_R_N_M_B_H_F_V_W__)))
                            {
                                Add("K", ref sbPrimary, ref sbSecondary);
                            }
                            else
                            {
                                if (n > 0)
                                {
                                    if (Match(word, 0, MC))
                                        // e.g., "McHugh"
                                        Add("K", ref sbPrimary, ref sbSecondary);
                                    else
                                        Add("X", "K", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                }
                                else
                                    Add("X", ref sbPrimary, ref sbSecondary);
                            }
                            n += 2;
                            break;
                        }

                        if (Match(word, n, CZ) && !Match(word, n - 2, WICZ))
                        {
                            Add("S", "X", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 2;
                            break;
                        }

                        if (Match(word, n + 1, CIA))
                        {
                            Add("X", ref sbPrimary, ref sbSecondary);
                            n += 3;
                            break;
                        }

                        if (Match(word, n, CC) && !((n == 1) && Match(word, 0, 'M')))
                        {
                            if (Match(word, n + 2, I_E_H) && !Match(word, n + 2, HU))
                            {
                                if (((n == 1) && Match(word, n - 1, 'A')) || Match(word, n - 1, UCCEE_UCCES))
                                    Add("KS", ref sbPrimary, ref sbSecondary);
                                else
                                    Add("X", ref sbPrimary, ref sbSecondary);
                                n += 3;
                                break;
                            }
                            Add("K", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, CK_CG_CQ))
                        {
                            Add("K", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, CI_CE_CY))
                        {
                            if (Match(word, n, CIO_CIE_CIA))
                                Add("S", "X", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            else
                                Add("S", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        Add("K", ref sbPrimary, ref sbSecondary);

                        if (Match(word, n + 1, _C__Q__G))
                            n += 3;
                        else
                            n += ((Match(word, n + 1, C_K_Q) && !Match(word, n + 1, CE_CI)) ? 2 : 1);
                        break;

                    case 'D':
                        if (Match(word, n, DG))
                        {
                            if (Match(word, n + 2, I_E_Y))
                            {
                                Add("J", ref sbPrimary, ref sbSecondary);
                                n += 3;
                                break;
                            }
                            Add("TK", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, DT_DD))
                        {
                            Add("T", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        Add("T", ref sbPrimary, ref sbSecondary);
                        n++;
                        break;
                    case 'F':
                        n += (Match(word, n + 1, 'F') ? 2 : 1);
                        Add("F", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'G':
                        if (Match(word, n + 1, 'H'))
                        {
                            if ((n > 0) && !IsVowel(word, n - 1))
                            {
                                Add("K", ref sbPrimary, ref sbSecondary);
                                n += 2;
                                break;
                            }

                            if (n < 3)
                            {
                                if (n == 0)
                                {
                                    Add(Match(word, n + 2, 'I') ? "J" : "K", ref sbPrimary, ref sbSecondary);
                                    n += 2;
                                    break;
                                }
                            }

                            if (((n > 1) && Match(word, n - 2, B_H_D)) || ((n > 2) && Match(word, n - 3, B_H_D)) || ((n > 3) && Match(word, n - 4, B_H)))
                            {
                                n += 2;
                                break;
                            }
                            if ((n > 2) && Match(word, n - 1, 'U') && Match(word, n - 3, C_G_L_R_T))
                            {
                                Add("F", ref sbPrimary, ref sbSecondary);
                            }
                            else if ((n > 0) && !Match(word, n - 1, 'I'))
                            {
                                Add("K", ref sbPrimary, ref sbSecondary);
                            }

                            n += 2;
                            break;
                        }

                        if (Match(word, n + 1, 'N'))
                        {
                            if ((n == 1) && IsVowel(word, 0) && !isSlavoGermanic)
                            {
                                Add("KN", "N", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            }
                            else
                            {
                                if (!Match(word, n + 2, EY) && !Match(word, n + 1, 'Y') && !isSlavoGermanic)
                                {
                                    Add("N", "KN", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                }
                                else
                                {
                                    Add("KN", ref sbPrimary, ref sbSecondary);
                                }
                            }
                            n += 2;
                            break;
                        }

                        if (Match(word, n + 1, LI) && !isSlavoGermanic)
                        {
                            Add("KL", "L", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 2;
                            break;
                        }

                        if ((n == 0) && Match(word, n + 1, Y_ES_EP_EB_EL_EY_IB_IL_IN_IE_EI_ER))
                        {
                            Add("K", "J", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 2;
                            break;
                        }

                        if (Match(word, n + 1, Y_ER) && !Match(word, 0, DANGER_RANGER_MANGER) && !Match(word, n - 1, E_I) && !Match(word, n - 1, RGY_OGY))
                        {
                            Add("K", "J", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 2;
                            break;
                        }

                        if (Match(word, n + 1, E_I_Y) || Match(word, n - 1, AGGI_OGGI))
                        {
                            if (Match(word, 0, VAN__VON__SCH) || Match(word, n + 1, ET))
                            {
                                Add("K", ref sbPrimary, ref sbSecondary);
                            }
                            else
                            {
                                if (Match(word, n + 1, IER))
                                    Add("J", ref sbPrimary, ref sbSecondary);
                                else
                                    Add("J", "K", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            }
                            n += 2;
                            break;
                        }

                        Add("K", ref sbPrimary, ref sbSecondary);
                        n += (Match(word, n + 1, 'G') ? 2 : 1);
                        break;
                    case 'H':
                        if (((n == 0) || IsVowel(word, n - 1)) && IsVowel(word, n + 1))
                        {
                            Add("H", ref sbPrimary, ref sbSecondary);
                            n += 2;
                        }
                        else
                        {
                            n++;
                        }
                        break;
                    case 'J':
                        if (Match(word, n, JOSE) || Match(word, 0, SAN_))
                        {
                            if (((n == 0) && Match(word, n + 4, ' ')) || Match(word, 0, SAN_))
                            {
                                Add("H", ref sbPrimary, ref sbSecondary);
                            }
                            else
                            {
                                Add("J", "H", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            }
                            n++;
                            break;
                        }

                        if ((n == 0) && !Match(word, n, JOSE))
                        {
                            Add("J", "A", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                        }
                        else
                        {
                            if (IsVowel(word, n - 1) && !isSlavoGermanic && Match(word, n + 1, A_O))
                            {
                                Add("J", "H", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            }
                            else
                            {
                                if (n == last)
                                {
                                    Add("J", " ", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                }
                                else
                                {
                                    if (!Match(word, n + 1, L_T_K_S_N_M_B_Z) && !Match(word, n - 1, S_K_L))
                                        Add("J", ref sbPrimary, ref sbSecondary);
                                }
                            }
                        }

                        n += (Match(word, n + 1, 'J') ? 2 : 1);
                        break;
                    case 'K':
                        n += (Match(word, n + 1, 'K') ? 2 : 1);
                        Add("K", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'L':
                        if (Match(word, n + 1, 'L'))
                        {
                            if (((n == length - 3) && Match(word, n - 1, ILLO_ILLA_ALLE)) || ((Match(word, last - 1, AS_OS) || Match(word, last, A_O)) && Match(word, n - 1, ALLE)))
                            {
                                Add("L", " ", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                n += 2;
                                break;
                            }
                            n += 2;
                        }
                        else
                        {
                            n++;
                        }
                        Add("L", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'M':
                        if ((Match(word, n - 1, UMB) && ((n + 1 == last) || Match(word, n + 2, ER))) || Match(word, n + 1, 'M'))
                        {
                            n += 2;
                        }
                        else
                        {
                            n++;
                        }
                        Add("M", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'N':
                        n += (Match(word, n + 1, 'N') ? 2 : 1);
                        Add("N", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'Ñ':
                        n++;
                        Add("N", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'P':
                        if (Match(word, n + 1, 'H'))
                        {
                            Add("F", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        n += (Match(word, n + 1, P_B) ? 2 : 1);
                        Add("P", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'Q':
                        n += (Match(word, n + 1, 'Q') ? 2 : 1);
                        Add("K", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'R':
                        if ((n == last) && !isSlavoGermanic && Match(word, n - 2, IE) && !Match(word, n - 4, ME_MA))
                        {
                            Add("", "R", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                        }
                        else
                        {
                            Add("R", ref sbPrimary, ref sbSecondary);
                        }

                        n += (Match(word, n + 1, 'R') ? 2 : 1);
                        break;
                    case 'S':
                        if (Match(word, n - 1, ISL_YSL))
                        {
                            n++;
                            break;
                        }

                        if ((n == 0) && Match(word, n, SUGAR))
                        {
                            Add("X", "S", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n++;
                            break;
                        }

                        if (Match(word, n, SH))
                        {
                            Add(Match(word, n + 1, HEIM_HOEK_HOLM_HOLZ) ? "S" : "X", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if (Match(word, n, SIO_SIA) || Match(word, n, SIAN))
                        {
                            if (!isSlavoGermanic)
                                Add("S", "X", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            else
                                Add("S", ref sbPrimary, ref sbSecondary);
                            n += 3;
                            break;
                        }

                        if (((n == 0) && Match(word, n + 1, M_N_L_W)) || Match(word, n + 1, 'Z'))
                        {
                            Add("S", "X", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += (Match(word, n + 1, 'Z') ? 2 : 1);
                            break;
                        }

                        if (Match(word, n, SC))
                        {
                            if (Match(word, n + 2, 'H'))
                            {
                                if (Match(word, n + 3, OO_ER_EN_UY_ED_EM))
                                {
                                    if (Match(word, n + 3, ER_EN))
                                        Add("X", "SK", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                    else
                                        Add("SK", ref sbPrimary, ref sbSecondary);
                                    n += 3;
                                    break;
                                }
                                if ((n == 0) && !IsVowel(word, 3) && !Match(word, 3, 'W'))
                                    Add("X", "S", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                                else
                                    Add("X", ref sbPrimary, ref sbSecondary);
                                n += 3;
                                break;
                            }

                            Add(Match(word, n + 2, I_E_Y) ? "S" : "SK", ref sbPrimary, ref sbSecondary);
                            n += 3;
                            break;
                        }

                        if ((n == last) && Match(word, n - 2, AI_OI))
                            Add("", "S", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                        else
                            Add("S", ref sbPrimary, ref sbSecondary);

                        n += (Match(word, n + 1, S_Z) ? 2 : 1);
                        break;
                    case 'T':
                        if (Match(word, n, TION))
                        {
                            Add("X", ref sbPrimary, ref sbSecondary);
                            n += 3;
                            break;
                        }

                        if (Match(word, n, TIA_TCH))
                        {
                            Add("X", ref sbPrimary, ref sbSecondary);
                            n += 3;
                            break;
                        }

                        if (Match(word, n, TH_TTH))
                        {
                            if (Match(word, n + 2, OM_AM) || Match(word, 0, VAN__VON__SCH))
                                Add("T", ref sbPrimary, ref sbSecondary);
                            else
                                Add("0", "T", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 2;
                            break;
                        }

                        n += (Match(word, n + 1, T_D) ? 2 : 1);
                        Add("T", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'V':
                        n += (Match(word, n + 1, 'V') ? 2 : 1);
                        Add("F", ref sbPrimary, ref sbSecondary);
                        break;
                    case 'W':
                        if (Match(word, n, WR))
                        {
                            Add("R", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }

                        if ((n == 0) && (IsVowel(word, n + 1) || Match(word, n, WH)))
                        {
                            if (IsVowel(word, n + 1))
                                Add("A", "F", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            else
                                Add("A", ref sbPrimary, ref sbSecondary);
                        }

                        if (((n == last) && IsVowel(word, n - 1)) || Match(word, n - 1, EWSKI_EWSKY_OWSKI_OWSKY) || Match(word, 0, SCH))
                        {
                            Add("", "F", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n++;
                            break;
                        }

                        if (Match(word, n, WICZ_WITZ))
                        {
                            Add("TS", "FX", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                            n += 4;
                            break;
                        }

                        n++;
                        break;
                    case 'X':
                        if (!((n == last) && (Match(word, n - 3, IAU_EAU) || Match(word, n - 2, AU_OU))))
                            Add("KS", ref sbPrimary, ref sbSecondary);

                        n += (Match(word, n + 1, C_X) ? 2 : 1);
                        break;
                    case 'Z':
                        if (Match(word, n + 1, 'H'))
                        {
                            Add("J", ref sbPrimary, ref sbSecondary);
                            n += 2;
                            break;
                        }
                        if (Match(word, n + 1, ZO_ZI_ZA) || (isSlavoGermanic && (n > 0) && !Match(word, n - 1, 'T')))
                        {
                            Add("S", "TS", ref sbPrimary, ref sbSecondary, ref hasAlternate);
                        }
                        else
                            Add("S", ref sbPrimary, ref sbSecondary);

                        n += (Match(word, n + 1, 'Z') ? 2 : 1);
                        break;
                    default:
                        n++;
                        break;
                }
            }

            if (MaxLength < 0)
            {
                if (hasAlternate)
                    return new[] { sbPrimary.ToString(), sbSecondary.ToString() };
                return new[] { sbPrimary.ToString() };
            }
            int primaryLength = Math.Min(MaxLength, sbPrimary.Length);
            if (hasAlternate)
            {
                int secondaryLength = Math.Min(MaxLength, sbSecondary.Length);
                return new[] { sbPrimary.ToString().Substring(0, (primaryLength) - (0)), sbSecondary.ToString().Substring(0, (secondaryLength) - (0)) };
            }
            return new[] { sbPrimary.ToString().Substring(0, (primaryLength) - (0)) };

        }

        public static IEnumerable<string> GetKeys(string phrase)
        {
            var set = new HashSet<string>();
            var keys = new Collection<string>();

            foreach (Match m in regex.Matches(phrase))
            {
                if (m.Value.Length > 2) set.Add(m.Value);
            }

            if (set.Count == 0) return keys;

            foreach (var word in set)
            {
                foreach (var key in BuildKeys(word))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }
    }
}
