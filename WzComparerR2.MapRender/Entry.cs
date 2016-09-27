﻿using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using DevComponents.DotNetBar;
using System.Threading;
using System.Windows.Forms;

namespace WzComparerR2.MapRender
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {

        }

        private RibbonBar bar;
        private ButtonItem btnItemMapRender;

        protected override void OnLoad()
        {
            this.bar = Context.AddRibbonBar("Modules", "MapRender");
            btnItemMapRender = new ButtonItem("", "맵 미리보기");
            btnItemMapRender.Click += btnItem_Click;
            bar.Items.Add(btnItemMapRender);
        }

        void btnItem_Click(object sender, EventArgs e)
        {
            btnItemMapRender.Enabled = false;
            Wz_Node node = Context.SelectedNode1;
            if (node != null)
            {
                Wz_Image img = node.Value as Wz_Image;
                Wz_File wzFile = node.GetNodeWzFile();

                if (img != null && img.TryExtract())
                {
                    if (wzFile == null || wzFile.Type != Wz_Type.Map)
                    {
                        if (MessageBoxEx.Show("Map.wz의 맵 img를 선택하지 않으셨습니다. 계속 진행하시겠습니까?", "경고", MessageBoxButtons.OKCancel) != DialogResult.OK)
                        {
                            goto exit;
                        }
                    }

                    StringLinker sl = this.Context.DefaultStringLinker;
                    if (!sl.HasValues) //生成默认stringLinker
                    {
                        sl = new StringLinker();
                        sl.Load(PluginManager.FindWz(Wz_Type.String).GetValueEx<Wz_File>(null));
                    }

                    //开始绘制
                    Thread thread = new Thread(() =>
                    {
#if !DEBUG
                        try
                        {
#endif
                            FrmMapRender frm = new MapRender.FrmMapRender(img);
                            frm.StringLinker = sl;
                            frm.Run();

#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            MessageBoxEx.Show(ex.ToString(), "MapRender");
                        }
#endif
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    goto exit;
                }
            }

            MessageBoxEx.Show("Map.wz에서 맵 img를 선택하세요.", "오류");

        exit:
            btnItemMapRender.Enabled = true;
        }

    }
}