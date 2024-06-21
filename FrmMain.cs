using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataGridOperationButtons
{
    public partial class FrmMain : Form
    {
        private bool isDefault = false;//想自定义改这里
        public FrmMain()
        {
            InitializeComponent();

            CreateColumns();
            //模拟，加多两行
            this.dataGridView1.Rows.Add();
            this.dataGridView1.Rows.Add();

            var ext = new DataGridViewExt(this.dataGridView1, "operate");//operate是列名

            //必须得把滚动事件安排上，不然不会重绘
            this.dataGridView1.Scroll += (sender, e) =>
            {
                ext.ReDrawOperateCtl(e);
            };

            for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                if (isDefault)
                {
                    ext.CreateOperateRow(i);
                }
                else
                {
                    var ctls = new List<Control>();

                    ctls.Add(new Button { Text="新增",Name="btnAdd" });
                    ctls.Add(new CheckBox { Name = "chkIsFind" });
                    ctls.Add(new Label { Text = "-", Name = "labDel" });

                    ext.CreateCustomOperateRow(i, ctls);

                }
            }


        }

        #region >设计列<
        private void CreateColumns()
        {
            DataGridViewColumn col = new DataGridViewImageColumn();

            col.DataPropertyName = "NeedManual";
            col.HeaderText = "状态";
            col.MinimumWidth = 6;
            col.Name = "needFixImg";
            col.DisplayIndex = 0;
            col.Width = 64;

            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "Name";
            col.HeaderText = "名称";
            col.MinimumWidth = 6;
            col.Name = "name";
            col.Width = 180;
            col.DisplayIndex = 1;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "DisplayName";
            col.HeaderText = "昵称";
            col.MinimumWidth = 6;
            col.Name = "displayName";
            col.Width = 180;
            col.DisplayIndex = 2;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewCheckBoxColumn();
            col.DataPropertyName = "EnableSync";
            col.HeaderText = "是否同步";
            col.MinimumWidth = 6;
            col.Name = "enableSync";
            col.Width = 80;
            col.DisplayIndex = 3;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "Total";
            col.HeaderText = "源表行数";
            col.MinimumWidth = 6;
            col.Name = "total";
            col.Width = 100;
            col.DisplayIndex = 4;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "Finish";
            col.HeaderText = "已同步";
            col.MinimumWidth = 6;
            col.Name = "finish";
            col.Width = 100;
            col.DisplayIndex = 5;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "Total2";
            col.HeaderText = "目标行数";
            col.MinimumWidth = 6;
            col.Name = "total2";
            col.Width = 100;
            col.DisplayIndex = 6;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewTextBoxColumn();
            col.DataPropertyName = "Description";
            col.HeaderText = "描述";
            col.MinimumWidth = 6;
            col.Name = "description";
            col.Width = 150;
            col.DisplayIndex = 7;
            this.dataGridView1.Columns.Add(col);



            col = new DataGridViewLinkColumn();
            //col.DataPropertyName = "Description";
            col.HeaderText = "操作";
            col.MinimumWidth = 6;
            col.Name = "operate";
            col.Width = 85;
            col.DisplayIndex = 8;
            this.dataGridView1.Columns.Add(col);

            col = new DataGridViewCheckBoxColumn();
            col.DataPropertyName = "IsNeedManual";
            col.HeaderText = "是否配置";
            col.MinimumWidth = 6;
            col.Name = "isNeedManual";
            col.Width = 125;
            col.Visible = false;//已经通过图标显示
            this.dataGridView1.Columns.Add(col);
        }

        #endregion
    }

    internal class DataGridViewExt
    {
        private DataGridView _dataGridView = null;
        private string _oprName = string.Empty;

        private List<List<Control>> _rowOprates = new List<List<Control>>();//把每行的控件记录一下，方便重绘

        public DataGridViewExt(DataGridView dataGridView, string oprName)
        {
            _dataGridView = dataGridView;
            _oprName = oprName;
        }

        //重绘操作控件
        public void ReDrawOperateCtl(ScrollEventArgs e)
        {
            //垂直方向
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                int curFirstIndex = _dataGridView.FirstDisplayedScrollingRowIndex;
                int disCount = _dataGridView.DisplayedRowCount(true);

                var ColumnIndex = this._dataGridView.Columns[_oprName].Index;

                HideAllOperate();//先隐藏，再计算那些需要显示

                for (int i = curFirstIndex; i < curFirstIndex + disCount; i++)
                {
                    Rectangle rect = _dataGridView.GetCellDisplayRectangle(ColumnIndex, i, true);

                    if (rect.Left > 0)
                    {
                        ShowOperates(rect, i);
                    }
                }
            }
            else  //水平方向
            {
                int curFirstIndex = _dataGridView.FirstDisplayedScrollingColumnIndex;
                int disCount = _dataGridView.DisplayedColumnCount(true);
                int ctlColumn = this._dataGridView.Columns[_oprName].Index; //控件所在列的位置

                if (ctlColumn < curFirstIndex + disCount)
                {
                    for (int i = 0; i < _rowOprates.Count; i++)
                    {
                        Rectangle rect = this._dataGridView.GetCellDisplayRectangle(ctlColumn, i, true);
                        ShowOperates(rect, i);
                    }
                }
                else
                {
                    HideAllOperate();
                }
            }

            //Sunny.UI.UIMessageTip.ShowOk("滑动了");
        }

        private void ShowOperates(Rectangle rect, int index)
        {
            var rowLabels = _rowOprates[index];
            var btnLeft = rect.Left;
            var btnSize = new Size((rect.Width / rowLabels.Count), rect.Height);

            foreach (var lab in rowLabels)
            {
                lab.Size = btnSize;
                lab.Location = new Point(btnLeft + 5, rect.Top + 5);
                btnLeft += lab.Width;
                lab.Visible = true;
            }
        }

        private void HideAllOperate()
        {
            for (int j = 0; j < _rowOprates.Count; j++)
            {
                var list = _rowOprates[j];

                foreach (var lab in list)
                {
                    lab.Visible = false;
                }
            }
        }


        public void CreateOperateRow(int rowIndex)
        {

            List<Control> rowLabels = new List<Control>();
            //配置按钮
            var btn = GetBtnByType("btnAdd", "新增", rowIndex);
            this._dataGridView.Controls.Add(btn);
            rowLabels.Add(btn);

            btn = GetBtnByType("btnModfiy", "修改", rowIndex);
            this._dataGridView.Controls.Add(btn);
            rowLabels.Add(btn);

            btn = GetBtnByType("btnDel", "删除", rowIndex, Color.Red);
            this._dataGridView.Controls.Add(btn);
            rowLabels.Add(btn);

            _rowOprates.Add(rowLabels);


            DrawControls(rowIndex, rowLabels);


        }

        //不用赋值size和localtion
        public void CreateCustomOperateRow(int rowIndex, List<Control> ctls)
        {
            _rowOprates.Add(ctls);

            foreach (var ctl in ctls)
            {
                this._dataGridView.Controls.Add(ctl);
            }

            DrawControls(rowIndex, ctls);
        }

        private void DrawControls(int rowIndex, List<Control> ctls)
        {
            int curFirstIndex = _dataGridView.FirstDisplayedScrollingColumnIndex;
            int disCount = _dataGridView.DisplayedColumnCount(true);
            int ctlColumn = this._dataGridView.Columns[_oprName].Index; //控件所在列的位置

            if (ctlColumn < curFirstIndex + disCount)//如果在可见区域内就显示
            {

                Rectangle rect = this._dataGridView.GetCellDisplayRectangle(ctlColumn, rowIndex, true);

                var btnLeft = rect.Left;
                var btnSize = new Size((rect.Width / ctls.Count), rect.Height);

                foreach (var lab in ctls)
                {
                    lab.Size = btnSize;
                    lab.Location = new Point(btnLeft + 5, rect.Top + 5);
                    btnLeft += lab.Width;
                }

            }
            else
            {
                foreach (var lab in ctls)
                {
                    lab.Visible = false;
                }
            }

        }

        public Label GetBtnByType(string btnName, string btnText, int rowIndex, Color? fontColor = null)
        {
            if (fontColor == null)
            {
                fontColor = Color.FromArgb(64, 158, 255);
            }

            var lab = new Label();
            lab.Name = btnName;
            lab.Text = btnText;
            lab.ForeColor = fontColor.Value;
            lab.Font = new Font("微软雅黑", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));

            lab.BackColor = Color.Transparent;
            lab.Tag = rowIndex;
            lab.AutoSize = true;
            //lab.Click += Lab_Click;

            return lab;
        }
    }
}
