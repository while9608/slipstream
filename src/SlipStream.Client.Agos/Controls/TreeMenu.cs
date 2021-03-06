﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using SlipStream.Client.Model;

namespace SlipStream.Client.Agos.Controls
{
    public class TreeMenu : TreeView
    {
        public TreeMenu()
        {
            this.DefaultStyleKey = typeof(TreeView);
            this.Loaded += new RoutedEventHandler(this.OnLoaded);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.Reload();
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
        }

        public void Reload()
        {
            var app = (App)Application.Current;

            app.ClientService.ReadAllMenus((menus, error) =>
            {
                this.LoadAllMenus(menus);
            });
        }

        private void LoadAllMenus(IEnumerable<MenuEntity> menus)
        {
            this.Items.Clear();
            this.InsertMenus(menus);
        }

        private void InsertMenus(IEnumerable<MenuEntity> menus)
        {
            var rootMenus =
                from m in menus
                where m.ParentId == null
                orderby m.Ordinal
                select m;

            foreach (var menu in rootMenus)
            {
                var node = InsertMenu(null, menu);

                InsertSubmenus(menus, menu, node);
            }
        }


        private TreeViewItem InsertMenu(TreeViewItem parent, MenuEntity menu)
        {
            var node = new TreeViewItem();
            node.Header = menu.Name;
            node.DataContext = menu;
            node.DisplayMemberPath = "Name";

            if (parent != null)
            {
                parent.Items.Add(node);
            }
            else
            {
                this.Items.Add(node);
            }
            return node;
        }

        private void InsertSubmenus(
            IEnumerable<MenuEntity> menus, MenuEntity parentMenu, TreeViewItem parentNode)
        {
            //子菜单们
            var submenus =
                from m in menus
                where m.ParentId != null && m.ParentId == parentMenu.Id
                orderby m.Ordinal
                select m;

            foreach (var menu in submenus)
            {
                var node = InsertMenu(parentNode, menu);
                //再把子子菜单们找出来
                InsertSubmenus(menus, menu, node);
            }
        }



    }
}
