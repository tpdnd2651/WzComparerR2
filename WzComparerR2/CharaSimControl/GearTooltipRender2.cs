﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSimControl
{
    public class GearTooltipRender2 : TooltipRender
    {
        static GearTooltipRender2()
        {
            res = new Dictionary<string, TextureBrush>();
            res["t"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_top, WrapMode.Clamp);
            res["line"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_line, WrapMode.Tile);
            res["dotline"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_dotline, WrapMode.Clamp);
            res["b"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_bottom, WrapMode.Clamp);
            res["cover"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_cover, WrapMode.Clamp);
        }

        private static Dictionary<string, TextureBrush> res;

        public GearTooltipRender2()
        {
        }

        private CharacterStatus charStat;

        public Gear Gear { get; set; }

        public override object TargetItem
        {
            get { return this.Gear; }
            set { this.Gear = value as Gear; }
        }

        public CharacterStatus CharacterStatus
        {
            get { return charStat; }
            set { charStat = value; }
        }

        public bool ShowSpeed { get; set; }
        public bool ShowLevelOrSealed { get; set; }
        public bool ShowMedalTag { get; set; } = true;

        public TooltipRender SetItemRender { get; set; }

        public override Bitmap Render()
        {
            if (this.Gear == null)
            {
                return null;
            }

            int[] picH = new int[4];
            Bitmap left = RenderBase(out picH[0]);
            Bitmap add = RenderAddition(out picH[1]);
            Bitmap set = RenderSetItem(out picH[2]);
            Bitmap levelOrSealed = null;
            if (this.ShowLevelOrSealed)
            {
                levelOrSealed = RenderLevelOrSealed(out picH[3]);
            }

            int width = 261;
            if (add != null) width += add.Width;
            if (set != null) width += set.Width;
            if (levelOrSealed != null) width += levelOrSealed.Width;
            int height = 0;
            for (int i = 0; i < picH.Length; i++)
            {
                height = Math.Max(height, picH[i]);
            }
            Bitmap tooltip = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制主图
            width = 0;
            if (left != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[0] - 13);
                g.DrawImage(res["b"].Image, width, picH[0] - 13);

                //复制图像
                g.DrawImage(left, width, 0, new Rectangle(0, 0, left.Width, picH[0]), GraphicsUnit.Pixel);

                //cover
                g.DrawImage(res["cover"].Image, 3, 3);

                width += left.Width;
                left.Dispose();
            }

            //绘制addition
            if (add != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, tooltip.Height - 13);
                g.DrawImage(res["b"].Image, width, tooltip.Height - 13);

                //复制原图
                g.DrawImage(add, width, 0, new Rectangle(0, 0, add.Width, picH[1]), GraphicsUnit.Pixel);

                width += add.Width;
                add.Dispose();
            }

            //绘制setitem
            if (set != null)
            {
                //绘制背景
                //g.DrawImage(res["t"].Image, width, 0);
                //FillRect(g, res["line"], width, 13, picH[2] - 13);
                //g.DrawImage(res["b"].Image, width, picH[2] - 13);

                //复制原图
                g.DrawImage(set, width, 0, new Rectangle(0, 0, set.Width, picH[2]), GraphicsUnit.Pixel);
                width += set.Width;
                set.Dispose();
            }

            //绘制levelOrSealed
            if (levelOrSealed != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[3] - 13);
                g.DrawImage(res["b"].Image, width, picH[3] - 13);

                //复制原图
                g.DrawImage(levelOrSealed, width, 0, new Rectangle(0, 0, levelOrSealed.Width, picH[3]), GraphicsUnit.Pixel);
                width += levelOrSealed.Width;
                levelOrSealed.Dispose();
            }

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Gear.ItemID.ToString("d8"), true);
            }

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderBase(out int picH)
        {
            Bitmap bitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            int value;

            picH = 13;
            DrawStar2(g, ref picH); //绘制星星

            //绘制装备名称
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringEqp.TryGetValue(Gear.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            string nameAdd = Gear.ScrollUp > 0 ? ("+" + Gear.ScrollUp) : null;
            switch (Gear.GetGender(Gear.ItemID))
            {
                case 0: nameAdd += "남"; break;
                case 1: nameAdd += "여"; break;
            }
            if (!string.IsNullOrEmpty(nameAdd))
            {
                gearName += " (" + nameAdd + ")";
            }

            format.Alignment = StringAlignment.Center;
            g.DrawString(gearName, GearGraphics.ItemNameFont2,
                GearGraphics.GetGearNameBrush(Gear.diff, Gear.ScrollUp > 0), 130, picH, format);
            picH += 23;
            format.Dispose();
            format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.Alignment = StringAlignment.Center;

            //装备rank
            string rankStr = null;
            if (Gear.GetBooleanValue(GearPropType.specialGrade))
            {
                rankStr = ItemStringHelper.GetGearGradeString(GearGrade.Special);
            }
            else if (!Gear.Cash) //T98后C级物品依然显示
            {
                rankStr = ItemStringHelper.GetGearGradeString(Gear.Grade);
            }
            if (rankStr != null)
            {
                g.DrawString(rankStr, GearGraphics.EquipDetailFont, Brushes.White, 130, picH, format);
                picH += 15;
            }

            //额外属性
            string attrStr = GetGearAttributeString();
            if (!string.IsNullOrEmpty(attrStr))
            {
                g.DrawString(attrStr, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush2, 130, picH, format);
                picH += 15;
            }

            //装备限时
            if (Gear.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("yyyy년 M월 d일 HH시 mm분까지 사용가능");
                g.DrawString(expireStr, GearGraphics.EquipDetailFont, Brushes.White, 130, picH, format);
                picH += 15;
            }

            //分割线1号
            picH += 7;
            g.DrawImage(res["dotline"].Image, 0, picH);

            //绘制装备图标
            if (Gear.Grade > 0 && (int)Gear.Grade <= 4) //绘制外框
            {
                Image border = Resource.ResourceManager.GetObject("UIToolTip_img_Item_ItemIcon_" + (int)Gear.Grade) as Image;
                if (border != null)
                {
                    g.DrawImage(border, 13, picH + 11);
                }
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, 12, picH + 10); //绘制背景
            if (Gear.IconRaw.Bitmap != null) //绘制icon
            {
                var attr = new System.Drawing.Imaging.ImageAttributes();
                var matrix = new System.Drawing.Imaging.ColorMatrix(
                    new[] {
                        new float[] { 1, 0, 0, 0, 0 },
                        new float[] { 0, 1, 0, 0, 0 },
                        new float[] { 0, 0, 1, 0, 0 },
                        new float[] { 0, 0, 0, 0.5f, 0 },
                        new float[] { 0, 0, 0, 0, 1 },
                        });
                attr.SetColorMatrix(matrix);

                //绘制阴影
                var shade = Resource.UIToolTip_img_Item_ItemIcon_shade;
                g.DrawImage(shade,
                    new Rectangle(18 + 9, picH + 15 + 54, shade.Width, shade.Height),
                    0, 0, shade.Width, shade.Height,
                    GraphicsUnit.Pixel,
                    attr);
                //绘制图标
                g.DrawImage(GearGraphics.EnlargeBitmap(Gear.IconRaw.Bitmap),
                    18 + (1 - Gear.IconRaw.Origin.X) * 2,
                    picH + 15 + (33 - Gear.IconRaw.Origin.Y) * 2);

                attr.Dispose();
            }
            if (Gear.Cash) //绘制cash标识
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_0),
                    18 + 68 - 26,
                    picH + 15 + 68 - 26);
            }
            //检查星岩
            bool hasSocket = Gear.GetBooleanValue(GearPropType.nActivatedSocket);
            if (hasSocket)
            {
                Bitmap socketBmp = GetAlienStoneIcon();
                if (socketBmp != null)
                {
                    g.DrawImage(GearGraphics.EnlargeBitmap(socketBmp),
                        18 + 2,
                        picH + 15 + 3);
                }
            }

            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, 16, picH + 14); //绘制左上角cover

            //绘制攻击力变化
            format.Alignment = StringAlignment.Far;
            g.DrawString("공격력 증가량", GearGraphics.EquipDetailFont, GearGraphics.GrayBrush2, 251, picH + 10, format);
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_incline_0, 249 - 19, picH + 27); //暂时画个0

            //绘制属性需求
            DrawGearReq(g, 97, picH + 58);
            picH += 93;

            //绘制属性变化
            DrawPropDiffEx(g, 12, picH);
            picH += 20;

            //绘制职业需求
            DrawJobReq(g, ref picH);

            //分割线2号
            g.DrawImage(res["dotline"].Image, 0, picH);
            picH += 8;

            bool hasPart2 = false;
            format.Alignment = StringAlignment.Center;

            //绘制属性
            if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
            {
                g.DrawString("슈페리얼", GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 130, picH, format);
                picH += 15;
            }
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                g.DrawString("突破上限武器", GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 130, picH, format);
                picH += 16;
            }

            //绘制装备升级
            if (Gear.Props.TryGetValue(GearPropType.level, out value) && !Gear.FixLevel)
            {
                bool max = (Gear.Levels != null && value >= Gear.Levels.Count);
                g.DrawString("성장 레벨 : " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 11, picH);
                picH += 15;
                g.DrawString("성장 경험치 : " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 11, picH);
                picH += 15;
            }

            if (Gear.Props.TryGetValue(GearPropType.@sealed, out value))
            {
                bool max = (Gear.Seals != null && value >= Gear.Seals.Count);
                g.DrawString("封印解除阶段 : " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 11, picH);
                picH += 16;
                g.DrawString("封印解除经验值 : " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 11, picH);
                picH += 16;
            }

            //绘制耐久度
            if (Gear.Props.TryGetValue(GearPropType.durability, out value))
            {
                g.DrawString("내구도 : " + "100%", GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 11, picH);
                picH += 15;
            }

            //装备类型
            bool isWeapon = Gear.IsLeftWeapon(Gear.type) || Gear.IsDoubleHandWeapon(Gear.type);
            string typeStr = ItemStringHelper.GetGearTypeString(Gear.type);
            if (!string.IsNullOrEmpty(typeStr))
            {
                if (isWeapon)
                {
                    typeStr = "무기분류 : " + typeStr;
                }
                else
                {
                    typeStr = "장비분류 : " + typeStr;
                }

                if (Gear.IsLeftWeapon(Gear.type) || Gear.type == GearType.katara)
                {
                    typeStr += " (한손무기)";
                }
                else if (Gear.IsDoubleHandWeapon(Gear.type))
                {
                    typeStr += " (두손무기)";
                }
                g.DrawString(typeStr, GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                picH += 15;
                hasPart2 = true;
            }
            
            if (!Gear.Props.TryGetValue(GearPropType.attackSpeed, out value) 
                && (Gear.IsWeapon(Gear.type) || Gear.type == GearType.katara)) //找不到攻速的武器
            {
                value = 6; //给予默认速度
            }
            //  if (gear.Props.TryGetValue(GearPropType.attackSpeed, out value) && value > 0)
            if (value > 0)
            {
                g.DrawString("공격속도 : " + ItemStringHelper.GetAttackSpeedString(value) + (ShowSpeed ? (" (" + value + ")") : null),
                    GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                picH += 15;
                hasPart2 = true;
            }
            //机器人等级
            if (Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
            {
                g.DrawString("등급 : " + value, GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                picH += 15;
                hasPart2 = true;
            }


            //一般属性
            List<GearPropType> props = new List<GearPropType>();
            foreach (KeyValuePair<GearPropType, int> p in Gear.Props)
            {
                if ((int)p.Key < 100 && p.Value != 0)
                    props.Add(p.Key);
            }
            props.Sort();
            bool epic = Gear.Props.TryGetValue(GearPropType.epicItem, out value) && value > 0;
            foreach (GearPropType type in props)
            {
                if (ItemStringHelper.GetGearPropString(type, Gear.Props[type]) != null)
                {
                    g.DrawString(ItemStringHelper.GetGearPropString(type, Gear.Props[type]),
                        (epic && Gear.IsEpicPropType(type)) ? GearGraphics.EpicGearDetailFont : GearGraphics.EquipDetailFont,
                        Brushes.White, 11, picH);
                    picH += 15;
                    hasPart2 = true;
                }
            }

            bool hasReduce = Gear.Props.TryGetValue(GearPropType.reduceReq, out value);
            if (hasReduce && value > 0)
            {
                g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.reduceReq, value), GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                picH += 15;
                hasPart2 = true;
            }

            bool hasTuc = Gear.HasTuc && Gear.Props.TryGetValue(GearPropType.tuc, out value);
            if (hasTuc)
            {
                g.DrawString("업그레이드 가능 횟수 : " + value, GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                picH += 15;
                hasPart2 = true;
            }
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0) //突破上限
            {
                g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.limitBreak, value), GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 11, picH);
                picH += 15;
                hasPart2 = true;
            }

            //星星锤子
            if (hasTuc && Gear.Hammer > -1 && Gear.GetMaxStar() > 0)
            {
                if (Gear.Hammer == 2)
                {
                    g.DrawString("黄金锤提炼完成", GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                    picH += 16;
                }
                if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0) //极真
                {
                    GearGraphics.DrawString(g, ItemStringHelper.GetGearPropString(GearPropType.superiorEqp, value), GearGraphics.EquipDetailFont, 13, 247, ref picH, 15, GearGraphics.GreenBrush2);
                }

                if (Gear.Props.TryGetValue(GearPropType.exceptUpgrade, out value) && value != 0)
                {
                    g.DrawString("강화불가", GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                    picH += 15;
                }
                else
                {
                    int maxStar = Gear.GetMaxStar();

                    if (Gear.Star > 0) //星星
                    {
                        g.DrawString(Gear.Star + "성 강화 적용 (최대 " + maxStar + "성)", GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                        picH += 15;
                    }
                    else
                    {
                        g.DrawString("최대 " + maxStar + "성까지 강화 가능", GearGraphics.EquipDetailFont, Brushes.White, 11, picH);
                        picH += 15;
                    }
                }
                /*picH += 2;
                g.DrawString("金锤子已提高的强化次数", GearGraphics.EquipDetailFont, GearGraphics.GoldHammerBrush, 11, picH);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(": " + Gear.Hammer.ToString() + (Gear.Hammer == 2 ? "(MAX)" : null), GearGraphics.TahomaFont, GearGraphics.GoldHammerBrush, 145, picH - 2);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                picH += 15;*/
                hasPart2 = true;
            }
            picH += 5;

            //绘制浮动属性
            if ((Gear.VariableStat != null && Gear.VariableStat.Count > 0) || hasReduce)
            {
                if (hasPart2) //分割线...
                {
                    picH -= 1;
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }

                if (Gear.VariableStat != null && Gear.VariableStat.Count > 0)
                {
                    int reqLvl;
                    Gear.Props.TryGetValue(GearPropType.reqLevel, out reqLvl);
                    g.DrawString("캐릭터 레벨 별 능력치 추가 (" + reqLvl + "Lv 까지)", GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 130, picH, format);
                    picH += 20;

                    int reduceLvl;
                    Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceLvl);

                    int curLevel = charStat == null ? reqLvl : Math.Min(charStat.Level, reqLvl);

                    foreach (var kv in Gear.VariableStat)
                    {
                        int dLevel = curLevel - reqLvl + reduceLvl;
                        //int addVal = (int)Math.Floor(kv.Value * dLevel);
                        //这里有一个计算上的错误 换方式执行
                        int addVal = (int)Math.Floor(double.Parse(kv.Value.ToString()) * dLevel);
                        string text = ItemStringHelper.GetGearPropString(kv.Key, addVal, 1);
                        text += string.Format(" ({0:f1} x {1})", kv.Value, dLevel);
                        g.DrawString(text, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 12, picH, StringFormat.GenericTypographic);
                        picH += 20;
                    }

                    if (hasReduce)
                    {
                        g.DrawString("업그레이드 및 강화 시, " + reqLvl + "Lv 무기로 취급", GearGraphics.EquipDetailFont, GearGraphics.GrayBrush2, 12, picH, StringFormat.GenericTypographic);
                        picH += 16;
                    }
                }
            }

            //绘制潜能
            int optionCount = 0;
            foreach (Potential potential in Gear.Options)
            {
                if (potential != null)
                {
                    optionCount++;
                }
            }

            if (optionCount > 0)
            {
                //分割线3号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.Grade), 8, picH - 1);
                g.DrawString("잠재옵션", GearGraphics.EquipDetailFont, GearGraphics.GetPotentialTextBrush(Gear.Grade), 26, picH);
                picH += 17;
                foreach (Potential potential in Gear.Options)
                {
                    if (potential != null)
                    {
                        g.DrawString(potential.ConvertSummary(), GearGraphics.EquipDetailFont2, Brushes.White, 11, picH);
                        picH += 16;
                    }
                }
                picH += 5;
            }

            if (hasSocket)
            {
                g.DrawLine(Pens.White, 6, picH, 254, picH);
                picH += 8;
                GearGraphics.DrawString(g, ItemStringHelper.GetGearPropString(GearPropType.nActivatedSocket, 1),
                    GearGraphics.EquipDetailFont, 11, 247, ref picH, 16);
                picH += 3;
            }

            //绘制附加潜能
            int adOptionCount = 0;
            foreach (Potential potential in Gear.AdditionalOptions)
            {
                if (potential != null)
                {
                    adOptionCount++;
                }
            }
            if (adOptionCount > 0)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.AdditionGrade), 8, picH - 1);
                g.DrawString("에디셔널 잠재옵션", GearGraphics.EquipDetailFont, GearGraphics.GetPotentialTextBrush(Gear.AdditionGrade), 26, picH);
                picH += 17;

                foreach (Potential potential in Gear.AdditionalOptions)
                {
                    if (potential != null)
                    {
                        g.DrawString("+ " + potential.ConvertSummary(), GearGraphics.EquipDetailFont2, Brushes.White, 11, picH);
                        picH += 15;
                    }
                }
                picH += 5;
            }

            //绘制desc
            List<string> desc = new List<string>();
            GearPropType[] descTypes = new GearPropType[]{ 
                GearPropType.tradeAvailable,
                GearPropType.accountShareTag,
                GearPropType.jokerToSetItem };
            foreach (GearPropType type in descTypes)
            {
                if (Gear.Props.TryGetValue(type, out value) && value != 0)
                {
                    desc.Add(ItemStringHelper.GetGearPropString(type, value));
                }
            }

            //绘制倾向
            if (Gear.State == GearState.itemList)
            {
                string incline = null;
                GearPropType[] inclineTypes = new GearPropType[]{
                    GearPropType.charismaEXP,
                    GearPropType.insightEXP,
                    GearPropType.willEXP,
                    GearPropType.craftEXP,
                    GearPropType.senseEXP,
                    GearPropType.charmEXP };

                string[] inclineString = new string[]{
                    "카리스마","통찰력","의지","손재주","감성","매력"};

                for (int i = 0; i < inclineTypes.Length; i++)
                {
                    if (Gear.Props.TryGetValue(inclineTypes[i], out value) && value > 0)
                    {
                        incline += ", " + inclineString[i] + " " + value;
                    }
                }

                if (!string.IsNullOrEmpty(incline))
                {
                    desc.Add("\n #c장착 시 1회에 한해 " + incline.Substring(2) + "의 경험치를 얻으실 수 있습니다.#");
                }
            }

            //判断是否绘制徽章
            Wz_Node medalResNode = null;
            bool willDrawMedalTag = this.ShowMedalTag
                && this.Gear.Props.TryGetValue(GearPropType.medalTag, out value)
                && this.TryGetMedalResource(value, out medalResNode);
            if (!string.IsNullOrEmpty(sr.Desc) || desc.Count > 0 || Gear.Sample.Bitmap != null || willDrawMedalTag)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                if (Gear.Sample.Bitmap != null)
                {
                    g.DrawImage(Gear.Sample.Bitmap, (bitmap.Width - Gear.Sample.Bitmap.Width) / 2, picH);
                    picH += Gear.Sample.Bitmap.Height;
                    picH += 4;
                }
                if (medalResNode != null)
                {
                    this.DrawMedalTag(g, medalResNode, sr.Name, ref picH);
                    picH += 4;
                }
                if (!string.IsNullOrEmpty(sr.Desc))
                {
                    GearGraphics.DrawString(g, sr.Desc, GearGraphics.EquipDetailFont2, 13, 247, ref picH, 15);
                }
                foreach (string str in desc)
                {
                    GearGraphics.DrawString(g, str, GearGraphics.EquipDetailFont, 13, 247, ref picH, 15);
                }
                picH += 5;
            }

            picH += 2;
            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private Bitmap RenderAddition(out int picHeight)
        {
            Bitmap addBitmap = null;
            picHeight = 0;
            if (Gear.Additions.Count > 0)
            {
                addBitmap = new Bitmap(261, DefaultPicHeight);
                Graphics g = Graphics.FromImage(addBitmap);
                StringBuilder sb = new StringBuilder();
                foreach (Addition addition in Gear.Additions)
                {
                    string conString = addition.GetConString(), propString = addition.GetPropString();
                    if (!string.IsNullOrEmpty(conString) || !string.IsNullOrEmpty(propString))
                    {
                        sb.Append("- ");
                        if (!string.IsNullOrEmpty(conString))
                            sb.AppendLine(conString);
                        if (!string.IsNullOrEmpty(propString))
                            sb.AppendLine(propString);
                        sb.AppendLine();
                    }
                }
                if (sb.Length > 0)
                {
                    picHeight = 10;
                    GearGraphics.DrawString(g, sb.ToString(), GearGraphics.EquipDetailFont, 10, 250, ref picHeight, 15);
                }
                g.Dispose();
            }
            return addBitmap;
        }

        private Bitmap RenderSetItem(out int picHeight)
        {
            Bitmap setBitmap = null;
            int setID;
            picHeight = 0;
            if (Gear.Props.TryGetValue(GearPropType.setItemID, out setID))
            {
                SetItem setItem;
                if (!CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                    return null;

                TooltipRender renderer = this.SetItemRender;
                if (renderer == null)
                {
                    var defaultRenderer = new SetItemTooltipRender();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }

                renderer.TargetItem = setItem;
                setBitmap = renderer.Render();
                if (setBitmap != null)
                    picHeight = setBitmap.Height;
            }
            return setBitmap;
        }

        private Bitmap RenderLevelOrSealed(out int picHeight)
        {
            Bitmap levelOrSealed = null;
            Graphics g = null;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            picHeight = 0;
            if (Gear.Levels != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                g.DrawString("레벨 정보", GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 130, picHeight, format);
                picHeight += 15;
                if (Gear.FixLevel)
                {
                    g.DrawString("[획득시 레벨 고정]", GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush, 130, picHeight, format);
                    picHeight += 16;
                }

                for (int i = 0; i < Gear.Levels.Count; i++)
                {
                    var info = Gear.Levels[i];
                    g.DrawString("레벨 " + info.Level + (i >= Gear.Levels.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 10, picHeight);
                    picHeight += 15;
                    foreach (var kv in info.BonusProps)
                    {
                        GearLevelInfo.Range range = kv.Value;

                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value.Min);
                        if (propString != null)
                        {
                            if (range.Max != range.Min)
                            {
                                propString += " ~ " + kv.Value.Max + (propString.EndsWith("%") ? "%" : null);
                            }
                            g.DrawString(propString, GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                            picHeight += 15;
                        }
                    }
                    if (info.Skills.Count > 0)
                    {
                        string title = string.Format("{2:P2}({0}/{1}) 확률로 스킬 강화 옵션 추가 :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        g.DrawString(title, GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 15;
                        foreach (var kv in info.Skills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("{0} +{2}레벨", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            g.DrawString(text, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush, 16, picHeight);
                            picHeight += 15;
                        }
                    }
                    if (info.EquipmentSkills.Count > 0)
                    {
                        string title;
                        if (info.Prob < info.ProbTotal)
                        {
                            title = string.Format("有 {2:P2}({0}/{1}) 的几率装备时获得技能 :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        }
                        else
                        {
                            title = "스킬 사용 가능 :";
                        }
                        g.DrawString(title, GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 15;
                        foreach (var kv in info.EquipmentSkills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("{0} {2}레벨", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            g.DrawString(text, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush, 16, picHeight);
                            picHeight += 15;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        g.DrawString("필요 경험치 : " + info.Exp + "%", GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 15;
                    }

                    picHeight += 2;
                }
            }

            if (Gear.Seals != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                g.DrawString("封印解除属性", GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 130, picHeight, format);
                picHeight += 16;
                for (int i = 0; i < Gear.Seals.Count; i++)
                {
                    var info = Gear.Seals[i];

                    g.DrawString("等级 " + info.Level + (i >= Gear.Seals.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, GearGraphics.GreenBrush2, 10, picHeight);
                    picHeight += 16;
                    foreach (var kv in info.BonusProps)
                    {
                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value);
                        g.DrawString(propString, GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 16;
                    }
                    if (info.HasIcon)
                    {
                        Bitmap icon = info.Icon.Bitmap ?? info.IconRaw.Bitmap;
                        if (icon != null)
                        {
                            g.DrawString("图标 : ", GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight + icon.Height / 2 - 6);
                            g.DrawImage(icon, 52, picHeight);
                            picHeight += icon.Height;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        g.DrawString("经验成长率 : " + info.Exp + "%", GearGraphics.EquipDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 16;
                    }
                    picHeight += 2;
                }
            }


            format.Dispose();
            if (g != null)
            {
                g.Dispose();
                picHeight += 13;
            }
            return levelOrSealed;
        }

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private string GetGearAttributeString()
        {
            int value;
            List<string> tags = new List<string>();

            if (Gear.Props.TryGetValue(GearPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.only, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.equipTradeBlock, out value) && value != 0)
            {
                if (Gear.State == GearState.itemList)
                {
                    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.equipTradeBlock, value));
                }
                else
                {
                    string tradeBlock = ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, 1);
                    if (!tags.Contains(tradeBlock))
                        tags.Add(tradeBlock);
                }
            }
            if (Gear.Props.TryGetValue(GearPropType.accountSharable, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.accountSharable, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.noPotential, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.noPotential, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.fixedPotential, value));
            }

            if (Gear.Props.TryGetValue(GearPropType.onlyEquip, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.onlyEquip, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.blockGoldHammer, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.blockGoldHammer, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.notExtend, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.notExtend, value));
            }

            return tags.Count > 0 ? string.Join(", ", tags.ToArray()) : null;
        }

        private Bitmap GetAlienStoneIcon()
        {
            if (Gear.AlienStoneSlot == null)
            {
                return Resource.ToolTip_Equip_AlienStone_Empty;
            }
            else
            {
                switch (Gear.AlienStoneSlot.Grade)
                {
                    case AlienStoneGrade.Normal:
                        return Resource.ToolTip_Equip_AlienStone_Normal;
                    case AlienStoneGrade.Rare:
                        return Resource.ToolTip_Equip_AlienStone_Rare;
                    case AlienStoneGrade.Epic:
                        return Resource.ToolTip_Equip_AlienStone_Epic;
                    case AlienStoneGrade.Unique:
                        return Resource.ToolTip_Equip_AlienStone_Unique;
                    case AlienStoneGrade.Legendary:
                        return Resource.ToolTip_Equip_AlienStone_Legendary;
                    default:
                        return null;
                }
            }
        }

        private void DrawGearReq(Graphics g, int x, int y)
        {
            int value;
            bool can;
            NumberType type;
            Size size;
            //需求等级
            this.Gear.Props.TryGetValue(GearPropType.reqLevel, out value);
            {
                int reduceReq;
                if (this.Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceReq))
                {
                    value = Math.Max(0, value - reduceReq);
                }
            }
            can = this.charStat == null || this.charStat.Level >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqLEV", out size), x, y);
            DrawReqNum(g, value.ToString().PadLeft(3), (type == NumberType.Can ? NumberType.YellowNumber : type), x + 54, y, StringAlignment.Near);

            //需求人气
            this.Gear.Props.TryGetValue(GearPropType.reqPOP, out value);
            can = this.charStat == null || this.charStat.Pop >= value;
            type = GetReqType(can, value);
            if (value > 0)
            {
                g.DrawImage(FindReqImage(type, "reqPOP", out size), x + 80, y);
                DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
            }

            y += 15;

            //需求力量
            this.Gear.Props.TryGetValue(GearPropType.reqSTR, out value);
            can = this.charStat == null || this.charStat.Strength.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqSTR", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);


            //需求运气
            this.Gear.Props.TryGetValue(GearPropType.reqLUK, out value);
            can = this.charStat == null || this.charStat.Luck.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqLUK", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);

            y += 9;

            //需求敏捷
            this.Gear.Props.TryGetValue(GearPropType.reqDEX, out value);
            can = this.charStat == null || this.charStat.Dexterity.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqDEX", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);

            //需求智力
            this.Gear.Props.TryGetValue(GearPropType.reqINT, out value);
            can = this.charStat == null || this.charStat.Intelligence.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqINT", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
        }

        private void DrawPropDiffEx(Graphics g, int x, int y)
        {
            int value;
            string numValue;
            //防御
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_pdd, x, y);
            x += 62;
            DrawReqNum(g, "0", NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            //魔防
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_mdd, x, y);
            x += 62;
            DrawReqNum(g, "0", NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            //boss伤
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_bdr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.bdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            //无视防御
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_igpddr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.imdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5 - 4, y + 6, StringAlignment.Far);
        }

        private void DrawJobReq(Graphics g, ref int picH)
        {
            int value;
            string extraReq = ItemStringHelper.GetExtraJobReqString(Gear.type) ??
                (Gear.Props.TryGetValue(GearPropType.reqSpecJob, out value) ? ItemStringHelper.GetExtraJobReqString(value) : null);
            Image jobImage = extraReq == null ? Resource.UIToolTip_img_Item_Equip_Job_normal : Resource.UIToolTip_img_Item_Equip_Job_expand;
            g.DrawImage(jobImage, 12, picH);

            int reqJob;
            Gear.Props.TryGetValue(GearPropType.reqJob, out reqJob);
            int[] origin = new int[] { 15, 7, 60, 7, 93, 7, 137, 7, 171, 7, 204, 7 };
            int[] origin2 = new int[] { 15, 7, 60, 7, 93, 7, 137, 7, 171, 7, 204, 7 };
            for (int i = 0; i <= 5; i++)
            {
                bool enable;
                if (i == 0)
                {
                    enable = reqJob <= 0;
                    if (reqJob == 0) reqJob = 0x1f;//0001 1111
                    if (reqJob == -1) reqJob = 0; //0000 0000
                }
                else
                {
                    enable = (reqJob & (1 << (i - 1))) != 0;
                }
                if (enable)
                {
                    enable = this.charStat == null || Character.CheckJobReq(this.charStat.Job, i);
                    Image jobImage2 = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_Job_" + (enable ? "enable" : "disable") + "_" + i.ToString()) as Image;
                    if (jobImage != null)
                    {
                        if (enable)
                            g.DrawImage(jobImage2, 12 + origin[i * 2], picH + origin[i * 2 + 1]);
                        else
                            g.DrawImage(jobImage2, 12 + origin2[i * 2], picH + origin2[i * 2 + 1]);
                    }
                }
            }
            if (extraReq != null)
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                g.DrawString(extraReq, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 130, picH + 24, format);
                format.Dispose();
            }
            picH += jobImage.Height + 9;
        }

        private Image FindReqImage(NumberType type, string req, out Size size)
        {
            Image image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + req) as Image;
            if (image != null)
                size = image.Size;
            else
                size = Size.Empty;
            return image;
        }

        private void DrawStar(Graphics g, ref int picH)
        {
            if (Gear.Star > 0)
            {
                int totalWidth = Gear.Star * 10 + (Gear.Star / 5 - 1) * 6;
                int dx = 130 - totalWidth / 2;
                for (int i = 0; i < Gear.Star; i++)
                {
                    g.DrawImage(Resource.UIToolTip_img_Item_Equip_Star_Star, dx, picH);
                    dx += 10;
                    if (i > 0 && i % 5 == 4)
                    {
                        dx += 6;
                    }
                }
                picH += 18;
            }
        }

        private void DrawStar2(Graphics g, ref int picH)
        {
            int maxStar = Gear.GetMaxStar();
            if (maxStar > 0)
            {
                for (int i = 0; i < maxStar; i += 15)
                {
                    int starLine = Math.Min(maxStar - i, 15);
                    int totalWidth = starLine * 10 + (starLine / 5 - 1) * 6;
                    int dx = 130 - totalWidth / 2;
                    for (int j = 0; j < starLine; j++)
                    {
                        g.DrawImage((i + j < Gear.Star) ?
                            Resource.UIToolTip_img_Item_Equip_Star_Star : Resource.UIToolTip_img_Item_Equip_Star_Star0,
                            dx, picH);
                        dx += 10;
                        if (j > 0 && j % 5 == 4)
                        {
                            dx += 6;
                        }
                    }
                    picH += 18;
                }
                picH -= 1;
            }
        }

        private NumberType GetReqType(bool can, int reqValue)
        {
            if (reqValue <= 0)
                return NumberType.Disabled;
            if (can)
                return NumberType.Can;
            else
                return NumberType.Cannot;
        }

        private void DrawReqNum(Graphics g, string numString, NumberType type, int x, int y, StringAlignment align)
        {
            if (g == null || numString == null || align == StringAlignment.Center)
                return;
            int spaceWidth = type == NumberType.LookAhead ? 3 : 6;
            bool near = align == StringAlignment.Near;

            for (int i = 0; i < numString.Length; i++)
            {
                char c = near ? numString[i] : numString[numString.Length - i - 1];
                Image image = null;
                Point origin = Point.Empty;
                switch (c)
                {
                    case ' ':
                        break;
                    case '+':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "plus") as Image;
                        break;
                    case '-':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "minus") as Image;
                        origin.Y = 3;
                        break;
                    case '%':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "percent") as Image;
                        break;
                    default:
                        if ('0' <= c && c <= '9')
                        {
                            image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + c) as Image;
                            if (c == '1' && type == NumberType.LookAhead)
                            {
                                origin.X = 1;
                            }
                        }
                        break;
                }

                if (image != null)
                {
                    if (near)
                    {
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x += image.Width + origin.X + 1;
                    }
                    else
                    {
                        x -= image.Width + origin.X;
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x -= 1;
                    }
                }
                else //空格补位
                {
                    x += spaceWidth * (near ? 1 : -1);
                }
            }
        }

        private Image GetAdditionalOptionIcon(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.B: return Resource.AdditionalOptionTooltip_rare;
                case GearGrade.A: return Resource.AdditionalOptionTooltip_epic;
                case GearGrade.S: return Resource.AdditionalOptionTooltip_unique;
                case GearGrade.SS: return Resource.AdditionalOptionTooltip_legendary;
            }
            return null;
        }

        private bool TryGetMedalResource(int medalTag, out Wz_Node resNode)
        {
            resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/medal/" + medalTag);
            return resNode != null;
        }

        private void DrawMedalTag(Graphics g, Wz_Node resNode, string medalName, ref int picH)
        {
            if (g == null || resNode == null)
                return;

            //加载资源和文本颜色
            var wce = new[] { "w", "c", "e" }.Select(n =>
            {
                var node = resNode.FindNodeByPath(n);
                if (node == null)
                {
                    return new BitmapOrigin();
                }
                return BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
            }).ToArray();

            Color color = Color.FromArgb(resNode.FindNodeByPath("clr").GetValueEx(-1));

            //测试y轴大小
            int offsetY = wce.Min(bmp => bmp.OpOrigin.Y);
            int height = wce.Max(bmp => bmp.Rectangle.Bottom);

            //测试宽度
            var font = GearGraphics.EquipDetailFont2;
            var fmt = StringFormat.GenericTypographic;
            int width = string.IsNullOrEmpty(medalName) ? 0 : (int)Math.Ceiling(g.MeasureString(medalName, font, 261, fmt).Width);
            int left = 130 - width / 2;
            int right = left + width;

            //开始绘制背景
            picH -= offsetY;
            if (wce[0].Bitmap != null)
            {
                g.DrawImage(wce[0].Bitmap, left - wce[0].Origin.X, picH - wce[0].Origin.Y);
            }
            if (wce[1].Bitmap != null) //不用拉伸 用纹理平铺 看运气
            {
                var brush = new TextureBrush(wce[1].Bitmap);
                Rectangle rect = new Rectangle(left, picH - wce[1].Origin.Y, right - left, brush.Image.Height);
                brush.TranslateTransform(rect.X, rect.Y);
                g.FillRectangle(brush, rect);
                brush.Dispose();
            }
            if (wce[2].Bitmap != null)
            {
                g.DrawImage(wce[2].Bitmap, right - wce[2].Origin.X, picH - wce[2].Origin.Y);
            }

            //绘制文字
            if (!string.IsNullOrEmpty(medalName))
            {
                var brush = new SolidBrush(color);
                g.DrawString(medalName, font, brush, 130 - width / 2, picH, fmt);
                brush.Dispose();
            }

            picH += height;
        }

        private enum NumberType
        {
            Can,
            Cannot,
            Disabled,
            LookAhead,
            YellowNumber,
        }
    }
}
