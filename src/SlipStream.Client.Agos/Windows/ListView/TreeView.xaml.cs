﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;


using SilverlightTable;
using SlipStream.Client.Agos.Models;
using SlipStream.Client.Agos;
using SlipStream.Client.Agos.Controls;

namespace SlipStream.Client.Agos.Windows.TreeView
{
    public partial class TreeView : UserControl
    {
        private static readonly Dictionary<string, Tuple<Type, IValueConverter>> ColumnTypeMapping
            = new Dictionary<string, Tuple<Type, IValueConverter>>()
        {
            {"id", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter() ) },
            {"int32", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"int64", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"double", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"decimal", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"chars", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"text", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter()) },
            {"boolean", new Tuple<Type, IValueConverter>(typeof(DataGridCheckBoxColumn), new RowIndexConverter()) },
            {"datetime", new Tuple<Type, IValueConverter>(typeof( DataGridTextColumn), new RowIndexConverter(new DateTimeFieldConverter())) },
            {"date", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter(new DateFieldConverter())) },
            {"time", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter(new TimeFieldConverter())) },
            {"many-to-one", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter(new ManyToOneFieldConverter())) },
            {"reference", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter(new ReferenceFieldConverter())) },
            {"enum", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new RowIndexConverter(new EnumFieldConverter())) },
        };

        private IDictionary<string, object> viewRecord;
        private readonly IList<string> fields = new List<string>();
        private string modelName;
        private readonly Dictionary<string, IQueryField> createdQueryFields =
            new Dictionary<string, IQueryField>();

        public TreeView()
        {
            this.InitializeComponent();
            this.AdvancedConditionsExpander.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Query()
        {
            this.LoadData();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Init(string model, long? viewID)
        {
            this.modelName = model;
            var app = (App)Application.Current;

            var getViewArgs = new object[] { this.modelName, "tree", viewID };
            app.ClientService.Execute("core.view", "GetView", getViewArgs, (result, error) =>
            {
                this.viewRecord = (IDictionary<string, object>)result;
                this.LoadInternal();
            });
        }

        private void LoadData()
        {
            Debug.Assert(this.createdQueryFields != null);

            var app = (App)Application.Current;
            //加载数据
            var offset = 0;// long.Parse(this.textOffset.Text);
            var limit = 2000;// long.Parse(this.textLimit.Text);

            //生成条件
            var constraints = new List<object[]>();
            foreach (var p in this.createdQueryFields)
            {
                if (!p.Value.IsEmpty)
                {
                    foreach (var c in p.Value.GetConstraints())
                    {
                        constraints.Add(c.ToConstraint());
                    }
                }
            }

            app.ClientService.SearchModel(this.modelName, constraints.ToArray(), null, offset, limit, (ids, searchError) =>
            {
                app.ClientService.ReadModel(this.modelName, ids, this.fields, (records, readError) =>
                {
                    //我们需要一个唯一的字符串型 ID
                    //this.gridList.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Dictionary<string, object>>(records);
                    //this.gridList.ItemsSource = DataSourceCreator.ToDataSource(records);
                    var data = new SilverlightTable.SortableCollectionView(
                        records.Select(r => new SilverlightTable.Row(r)));
                    this.gridList.ItemsSource = data;
                });
            });
        }

        private void LoadInternal()
        {
            var layout = (string)this.viewRecord["layout"];
            var layoutDocument = XDocument.Parse(layout);

            this.InitializeColumns(layoutDocument);

            this.LoadData();
        }

        private void InitializeColumns(XDocument layoutDocument)
        {
            var app = (App)Application.Current;
            var args = new object[] { null };
            app.ClientService.Execute(this.modelName, "GetFields", args, (result, error) =>
            {
                var metaFields = ((object[])result).Select(r => (Dictionary<string, object>)r).ToArray();
                var viewFields = layoutDocument.Elements("tree").Elements();

                IList<DataGridBoundColumn> cols = new List<DataGridBoundColumn>();
                cols.Add(this.MakeColumn("_id", "id", "ID", System.Windows.Visibility.Collapsed));

                foreach (var f in viewFields)
                {
                    var fieldName = f.Attribute("name").Value;
                    var metaField = metaFields.Single(i => (string)i["name"] == fieldName);
                    cols.Add(this.MakeColumn(fieldName, (string)metaField["type"], (string)metaField["label"]));
                }

                this.gridList.Columns.Clear();
                foreach (var col in cols)
                {
                    this.gridList.Columns.Add(col);
                }

                this.BasicConditions.Child = (Grid)this.CreateQueryForm(metaFields, viewFields, "basic");

                if (viewFields.Select(ele => ele.Attribute("where"))
                    .Any(attr => attr != null && attr.Value == "advanced"))
                {
                    this.AdvancedConditionsExpander.Visibility = System.Windows.Visibility.Visible;
                    this.AdvancedConditionsExpander.DataContext
                        = (Grid)this.CreateQueryForm(metaFields, viewFields, "advanced");
                }
                else
                {
                    this.AdvancedConditionsExpander.Visibility = System.Windows.Visibility.Collapsed;
                }

                this.ClearAllConstraints();
            });
        }

        private object CreateQueryForm(Dictionary<string, object>[] fields, IEnumerable<XElement> viewFields, string where)
        {
            //生成基本查询条件表单
            var columnsPerRow = 6;// (int)Math.Round(this.Width / 150.00) * 2;
            if (columnsPerRow % 2 != 0) columnsPerRow--;
            var basicQueryForm = new Sandwych.Layout.Models.Form()
            {
                ColumnCount = columnsPerRow,
            };

            var basicFields = viewFields.Where(ele =>
            {
                var attr = ele.Attribute("where");
                return attr != null && attr.Value == where;
            });

            var basicQueryFormChildren = new List<Sandwych.Layout.Models.Placable>();
            var factory = new QueryFieldControlFactory(fields);
            var createdFieldControls = new Dictionary<string, IQueryField>();
            foreach (var fieldLayout in basicFields)
            {
                var fieldName = fieldLayout.Attribute("name").Value;
                var metaField = fields.Single(i => (string)i["name"] == fieldName);
                var label = new Sandwych.Layout.Models.Label()
                {
                    Text = (string)metaField["label"],
                };
                basicQueryFormChildren.Add(label);

                var field = new Sandwych.Layout.Models.Input()
                {
                    Field = (string)metaField["name"],
                };
                basicQueryFormChildren.Add(field);
            }
            basicQueryForm.ChildElements = basicQueryFormChildren.ToArray();

            var layoutEngine = new Sandwych.Layout.LayoutEngine(factory);
            var basicQueryGrid = layoutEngine.BuildLayout(basicQueryForm);

            foreach (var p in factory.CreatedQueryFields)
            {
                this.createdQueryFields.Add(p.Key, p.Value);
            }

            return basicQueryGrid;
        }


        private DataGridBoundColumn MakeColumn(
            string fieldName, string fieldType, string fieldLabel, Visibility visibility = Visibility.Visible)
        {
            this.fields.Add(fieldName);
            var tuple = ColumnTypeMapping[fieldType];
            var col = Activator.CreateInstance(tuple.Item1) as DataGridBoundColumn;
            col.Visibility = visibility;
            col.Header = fieldLabel;
            col.CanUserSort = true;
            col.CanUserResize = true;
            col.CanUserReorder = true;
            col.SortMemberPath = fieldName;

            var binding = new System.Windows.Data.Binding("ColumnValue")
            {
                ConverterParameter = fieldName,
                Converter = tuple.Item2,
            };
            col.Binding = binding;

            return col;
        }

        public void DeleteSelectedItems()
        {
            var sb = new System.Text.StringBuilder();
            var ids = new List<long>();
            foreach (dynamic item in this.gridList.SelectedItems)
            {
                var id = (long)item._id;
                ids.Add(id);
            }

            var msg = String.Format("您确定要永久删除 {0} 条记录吗？", ids.Count);
            var dlgResult = MessageBox.Show(msg, "删除确认", MessageBoxButton.OKCancel);

            if (dlgResult == MessageBoxResult.OK)
            {
                //执行删除
                var app = (App)Application.Current;
                app.IsBusy = true;

                var args = new object[] { ids };
                app.ClientService.Execute(this.modelName, "Delete", args, (result, error) =>
                {
                    this.LoadData();
                    app.IsBusy = false;
                });
            }
        }

        private void ClearConstraintsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClearAllConstraints();
            this.LoadData();
        }

        public void ClearAllConstraints()
        {
            System.Diagnostics.Debug.Assert(this.createdQueryFields != null);

            foreach (var p in this.createdQueryFields)
            {
                p.Value.Empty();
            }
        }

        #region IsQueryable
        public bool IsQueryable
        {
            get { return (bool)GetValue(IsQueryableProperty); }
            set { SetValue(IsQueryableProperty, value); }
        }

        /// <summary>
        /// Identifies the
        /// <see cref="P:System.Windows.Controls.DatePicker.IsDropDownOpen" />
        /// dependency property.
        /// </summary>
        /// <value>
        /// The identifier for the
        /// <see cref="P:System.Windows.Controls.DatePicker.IsDropDownOpen" />
        /// dependency property.
        /// </value>
        public static readonly DependencyProperty IsQueryableProperty =
            DependencyProperty.Register(
            "IsQueryable",
            typeof(bool),
            typeof(TreeView),
            new PropertyMetadata(true, OnIsDropDownOpenChanged));

        /// <summary>
        /// IsDropDownOpenProperty property changed handler.
        /// </summary>
        /// <param name="d">DatePicker that changed its IsDropDownOpen.</param>
        /// <param name="e">The DependencyPropertyChangedEventArgs.</param>
        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as TreeView;
            Debug.Assert(self != null, "The TreeView should not be null!");
            bool newValue = (bool)e.NewValue;
            bool oldValue = (bool)e.OldValue;

            if (newValue)
            {
                self.QueryConditionsArea.Visibility = Visibility.Visible;
            }
            else
            {
                self.QueryConditionsArea.Visibility = Visibility.Collapsed;
            }

        }

        #endregion

        public long[] GetSelectedIDs()
        {
            var ids = new List<long>();
            foreach (dynamic item in this.gridList.SelectedItems)
            {
                var id = (long)item.ColumnValue["_id"];
                ids.Add(id);
            }
            return ids.ToArray();
        }

        public void Clear()
        {
            this.gridList.ItemsSource = null;
        }

    }
}
