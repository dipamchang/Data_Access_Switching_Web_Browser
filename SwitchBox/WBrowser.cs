
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Net.NetworkInformation;

namespace WBrowser
{
    public partial class WBrowser : Form
    {
        public static String favXml = "favorits.xml", linksXml = "links.xml", routes="routes.xml";
        String settingsXml="settings.xml", historyXml="history.xml";
        List<String> urls = new List<String>();
        XmlDocument settings = new XmlDocument();
        String homePage;
        CultureInfo currentCulture;

        public WBrowser()
        {
            InitializeComponent();
            currentCulture = CultureInfo.CurrentCulture;


            secondaryConnectionToolStripMenuItem.Enabled = false;
           // primaryConnectionToolStripMenuItem.Visible = true;
            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {

                if (netif.OperationalStatus == OperationalStatus.Up)
                {
                    ToolStripMenuItem itempc = new ToolStripMenuItem();
                    string name = netif.Name.ToString();
                    itempc.Name = (name);
                    itempc.Text = (name);
                    itempc.CheckOnClick = true;
                    //MessageBox.Show("done");
                    primaryConnectionToolStripMenuItem.DropDownItems.Add(itempc);
                   
                }

            }

            foreach (ToolStripMenuItem item in primaryConnectionToolStripMenuItem.DropDownItems)
            {
                string name = item.Name;
                string ip = ipofprimary(name);
                if(ip.StartsWith("10."))
                { item.Checked = true;
                break;
                }
                    
            }
            

          }

        public void UncheckOtherToolStripMenuItems(ToolStripMenuItem selectedMenuItem)
        {
            selectedMenuItem.Checked = true;

            // Select the other MenuItens from the ParentMenu(OwnerItens) and unchecked this,
            // The current Linq Expression verify if the item is a real ToolStripMenuItem
            // and if the item is a another ToolStripMenuItem to uncheck this.
            foreach (var ltoolStripMenuItem in (from object
                                                    item in selectedMenuItem.Owner.Items
                                                let ltoolStripMenuItem = item as ToolStripMenuItem
                                                where ltoolStripMenuItem != null
                                                where !item.Equals(selectedMenuItem)
                                                select ltoolStripMenuItem))
                (ltoolStripMenuItem).Checked = false;

            // This line is optional, for show the mainMenu after click
            selectedMenuItem.Owner.Show();
        }















        #region Form load/Closing/Closed
       
//visible items
        private void setVisibility()
        {
            if (!File.Exists(settingsXml))
            {
                XmlElement r = settings.CreateElement("settings");
                settings.AppendChild(r);
                XmlElement el ;
                
                el=settings.CreateElement("menuBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("adrBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("linkBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("favoritesPanel");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("SplashScreen");
                el.SetAttribute("checked", "True");
                r.AppendChild(el);

                 el = settings.CreateElement("homepage");
                el.InnerText="about:blank";
                r.AppendChild(el);

                el = settings.CreateElement("dropdown");
                el.InnerText = "15";
                r.AppendChild(el);
            }
            else
            {
                settings.Load(settingsXml);
                XmlElement r = settings.DocumentElement;
                menuBar.Visible = (r.ChildNodes[0].Attributes[0].Value.Equals("True"));
                adrBar.Visible = (r.ChildNodes[1].Attributes[0].Value.Equals("True"));
                linkBar.Visible=(r.ChildNodes[2].Attributes[0].Value.Equals("True"));
                favoritesPanel.Visible = (r.ChildNodes[3].Attributes[0].Value.Equals("True"));
                splashScreenToolStripMenuItem.Checked = (r.ChildNodes[4].Attributes[0].Value.Equals("True"));
                homePage=r.ChildNodes[5].InnerText;
            }

            this.linksBarToolStripMenuItem.Checked = linkBar.Visible;
            this.menuBarToolStripMenuItem.Checked = menuBar.Visible;
            this.commandBarToolStripMenuItem.Checked = adrBar.Visible;
            splashScreenToolStripMenuItem.Checked = (settings.DocumentElement.ChildNodes[4].Attributes[0].Value.Equals("True"));
            homePage = settings.DocumentElement.ChildNodes[5].InnerText;
        }
        // form load
        private void Form1_Load(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Text = "Done";
            comboBox1.SelectedItem = comboBox1.Items[0];
            setVisibility();
            addNewTab();
            if (splashScreenToolStripMenuItem.Checked == true)
                (new About(true)).Show(); 
        }
        //form closing
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (browserTabControl.TabCount != 2)
            {
                DialogResult dlg_res = (new Close()).ShowDialog();

                if (dlg_res == DialogResult.No) { e.Cancel = true; closeTab(); }
                else if (dlg_res == DialogResult.Cancel) e.Cancel = true;
                else Application.ExitThread();
                clearroute();
                MessageBox.Show("All Routes Deleted.");
            }
        }
        //form closed
        private void WBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            clearroute();
            MessageBox.Show("All Routes Deleted.");
            settings.Save(settingsXml);
            File.Delete("source.txt");
        }

         #endregion

        #region FAVORITES,LINKS,HISTORY METHODS 

        //addFavorit method
        private void addFavorit(String url, string name)
        {
            XmlDocument myXml = new XmlDocument();
            XmlElement el = myXml.CreateElement("favorit");
            el.SetAttribute("url", url);
            el.InnerText = name;
            if (!File.Exists(favXml))
            {
                XmlElement root = myXml.CreateElement("favorites");
                myXml.AppendChild(root);
                root.AppendChild(el);
            }
            else
            {
                myXml.Load(favXml);
                myXml.DocumentElement.AppendChild(el);
            }
            if (favoritesPanel.Visible == true)
            {
                TreeNode node = new TreeNode(el.InnerText, faviconIndex(el.GetAttribute("url")), faviconIndex(el.GetAttribute("url")));
                node.ToolTipText = el.GetAttribute("url");
                node.Name = el.GetAttribute("url");
                node.ContextMenuStrip = favContextMenu;
                favTreeView.Nodes.Add(node);
            }
            myXml.Save(favXml);
        }
        //addLink method
        private void addLink(String url, string name)
        {
            XmlDocument myXml = new XmlDocument();
            XmlElement el = myXml.CreateElement("link");
            el.SetAttribute("url", url);
            el.InnerText = name;

            if (!File.Exists(linksXml))
            {
                XmlElement root = myXml.CreateElement("links");
                myXml.AppendChild(root);
                root.AppendChild(el);
            }
            else
            {
                myXml.Load(linksXml);
                myXml.DocumentElement.AppendChild(el);
            }
            if (linkBar.Visible == true)
            {
                ToolStripButton b =
                          new ToolStripButton(el.InnerText, getFavicon(url), items_Click, el.GetAttribute("url"));
                b.ToolTipText = el.GetAttribute("url");
                b.MouseUp += new MouseEventHandler(b_MouseUp);
                linkBar.Items.Add(b);
            }

            if (favoritesPanel.Visible == true)
            {
                TreeNode node = new TreeNode(el.InnerText, faviconIndex(url), faviconIndex(el.GetAttribute("url")));
                node.Name = el.GetAttribute("url");
                node.ToolTipText = el.GetAttribute("url");
                node.ContextMenuStrip = linkContextMenu;
                favTreeView.Nodes[0].Nodes.Add(node);
            }
            myXml.Save(linksXml);
        }
        //delete link method
        private void deleteLink()
        {
             if (favoritesPanel.Visible == true)
                favTreeView.Nodes[0].Nodes[adress].Remove();
             if (linkBar.Visible == true)
                 linkBar.Items.RemoveByKey(adress);
            XmlDocument myXml = new XmlDocument();
            myXml.Load(linksXml);
            XmlElement root = myXml.DocumentElement;
            foreach (XmlElement x in root.ChildNodes)
            {
                if (x.GetAttribute("url").Equals(adress))
                {
                    root.RemoveChild(x);
                    break;
                }
            }

            myXml.Save(linksXml);
        }
        //renameLink method
        private void renameLink()
        {
            RenameLink rl = new RenameLink(name);
            if (rl.ShowDialog() == DialogResult.OK)
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(linksXml);
                foreach (XmlElement x in myXml.DocumentElement.ChildNodes)
                {
                    if (x.InnerText.Equals(name))
                    {
                        x.InnerText = rl.newName.Text;
                        break;
                    }
                }
                if(linkBar.Visible==true)
                  linkBar.Items[adress].Text = rl.newName.Text;
                if(favoritesPanel.Visible==true)
                favTreeView.Nodes[0].Nodes[adress].Text = rl.newName.Text;
                myXml.Save(linksXml);
            }
            rl.Close();
        }
        //delete favorit method
        private void deleteFavorit()
        {
            favTreeView.SelectedNode.Remove();

            XmlDocument myXml = new XmlDocument();
            myXml.Load(favXml);
            XmlElement root = myXml.DocumentElement;
            foreach (XmlElement x in root.ChildNodes)
            {
                if (x.GetAttribute("url").Equals(adress))
                {
                    root.RemoveChild(x);
                    break;
                }
            }

            myXml.Save(favXml);

        }
        //renameFavorit method
        private void renameFavorit()
        {
            RenameLink rl = new RenameLink(name);
            if (rl.ShowDialog() == DialogResult.OK)
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(favXml);
                foreach (XmlElement x in myXml.DocumentElement.ChildNodes)
                {
                    if (x.InnerText.Equals(name))
                    {
                        x.InnerText = rl.newName.Text;
                        break;
                    }
                }
                favTreeView.Nodes[adress].Text = rl.newName.Text;
                myXml.Save(favXml);
            }
            rl.Close();
        }

        //addHistory method
        private void addHistory(Uri url,string data)
        {
            XmlDocument myXml = new XmlDocument();
            int i=1;
            XmlElement el = myXml.CreateElement("item");
            el.SetAttribute("url", url.ToString());
            el.SetAttribute("lastVisited", data);

            if (!File.Exists(historyXml))
            {
                XmlElement root = myXml.CreateElement("history");
                myXml.AppendChild(root);
                el.SetAttribute("times", "1");
                root.AppendChild(el);
            }
            else
            {
                myXml.Load(historyXml);

                foreach (XmlElement x in myXml.DocumentElement.ChildNodes)
                {
                    if (x.GetAttribute("url").Equals(url.ToString()))
                    {
                        i = int.Parse(x.GetAttribute("times")) + 1;
                        myXml.DocumentElement.RemoveChild(x);
                        break;
                    }
                }

                el.SetAttribute("times", i.ToString());
                myXml.DocumentElement.InsertBefore(el, myXml.DocumentElement.FirstChild);

                if (favoritesPanel.Visible == true)
                {
                    /*ordered visited today*/
                    if (comboBox1.Text.Equals("Ordered Visited Today"))
                    {
                        if (!historyTreeView.Nodes.ContainsKey(url.ToString()))
                        {
                            TreeNode node =
                                 new TreeNode(url.ToString(), 3, 3);
                            node.ToolTipText = url.ToString() + "\nLast Visited: " + data + "\nTimes visited :" + i.ToString();
                            node.Name = url.ToString();
                            node.ContextMenuStrip = histContextMenu;
                            historyTreeView.Nodes.Insert(0, node);
                        }
                        else
                            historyTreeView.Nodes[url.ToString()].ToolTipText
                              = url.ToString() + "\nLast Visited: " + data + "\nTimes visited: " + i.ToString();
                    }
                    /*view by site*/
                    if (comboBox1.Text.Equals("View By Site"))
                    {
                        if (!historyTreeView.Nodes.ContainsKey(url.Host.ToString()))
                        {
                            historyTreeView.Nodes.Add(url.Host.ToString(), url.Host.ToString(), 0, 0);

                            TreeNode node =
                                   new TreeNode(url.ToString(), 3, 3);
                            node.ToolTipText = url.ToString() + "\nLast Visited: " + data + "\nTimes visited: " + i.ToString();
                            node.Name = url.ToString();
                            node.ContextMenuStrip = histContextMenu;
                            historyTreeView.Nodes[url.Host.ToString()].Nodes.Add(node);
                        }

                        else
                            if (!historyTreeView.Nodes[url.Host.ToString()].Nodes.ContainsKey(url.ToString()))
                            {
                                TreeNode node =
                                    new TreeNode(url.ToString(), 3, 3);
                                node.ToolTipText = url.ToString() + "\nLast Visited: " + data + "\nTimes visited: " + i.ToString();
                                node.Name = url.ToString();
                                node.ContextMenuStrip = histContextMenu;
                                historyTreeView.Nodes[url.Host.ToString()].Nodes.Add(node);
                            }
                            else
                                historyTreeView.Nodes[url.Host.ToString()].Nodes[url.ToString()].ToolTipText
                                        = url.ToString() + "\nLast Visited: " + data + "\nTimes visited" + i.ToString();

                    }
                    /* view by date*/
                    if (comboBox1.Text.Equals("View by Date"))
                    {
                        if (historyTreeView.Nodes[4].Nodes.ContainsKey(url.ToString()))
                            historyTreeView.Nodes[url.ToString()].ToolTipText
                                    = url.ToString() + "\nLast Visited: " + data + "\nTimes visited: " + i.ToString();
                        else
                        {
                            TreeNode node =
                                new TreeNode(url.ToString(), 3, 3);
                            node.ToolTipText = url.ToString() + "\nLast Visited: " + data + "\nTimes visited :" + i.ToString();
                            node.Name = url.ToString();
                            node.ContextMenuStrip = histContextMenu;
                            historyTreeView.Nodes[4].Nodes.Add(node);
                        }
                    }
                }

            } 
            myXml.Save(historyXml);
        }
//delete history
        private void deleteHistory()
        {
            XmlDocument myXml = new XmlDocument();
            myXml.Load(historyXml);
            XmlElement root = myXml.DocumentElement;
            foreach (XmlElement x in root.ChildNodes)
            {
                if (x.GetAttribute("url").Equals(adress))
                {
                    root.RemoveChild(x);
                    break;
                }
            }
            historyTreeView.SelectedNode.Remove();
            myXml.Save(historyXml);
        }

        #endregion

        #region TABURI
        /*TAB-uri*/

        //addNewTab method
        private void addNewTab()
        {
            TabPage tpage = new TabPage();
            tpage.BorderStyle = BorderStyle.Fixed3D;
            browserTabControl.TabPages.Insert(browserTabControl.TabCount - 1, tpage);
            WebBrowser browser = new WebBrowser();
            browser.Navigate(homePage);   
            tpage.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
            browserTabControl.SelectTab(tpage);
            browser.ProgressChanged += new WebBrowserProgressChangedEventHandler(Form1_ProgressChanged);
            browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(Form1_DocumentCompleted);
            browser.Navigating += new WebBrowserNavigatingEventHandler(Form1_Navigating);
            browser.CanGoBackChanged += new EventHandler(browser_CanGoBackChanged);
            browser.CanGoForwardChanged += new EventHandler(browser_CanGoForwardChanged);
           
        }

       
        //DocumentCompleted
        private void Form1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser currentBrowser = getCurrentBrowser();
            this.toolStripStatusLabel1.Text = "Done";
            String text = "Blank Page";

            if (!currentBrowser.Url.ToString().Equals("about:blank"))
            {
                text = currentBrowser.Url.Host.ToString();
            }

            this.adrBarTextBox.Text = currentBrowser.Url.ToString();
            browserTabControl.SelectedTab.Text = text;

            img.Image = favicon(currentBrowser.Url.ToString(), "net.png");

            if (!urls.Contains(currentBrowser.Url.Host.ToString()))
                urls.Add(currentBrowser.Url.Host.ToString());

            if (!currentBrowser.Url.ToString().Equals("about:blank") && currentBrowser.StatusText.Equals("Done"))
                addHistory(currentBrowser.Url,DateTime.Now.ToString(currentCulture));
        }
        //ProgressChanged    
        private void Form1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            if (e.CurrentProgress < e.MaximumProgress)
                toolStripProgressBar1.Value=(int)e.CurrentProgress;
            else toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;

        }
        //Navigating
        private void Form1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            this.toolStripStatusLabel1.Text = getCurrentBrowser().StatusText;

        }
        //closeTab method
        private void closeTab()
        {
            if (browserTabControl.TabCount != 2)
            {
                browserTabControl.TabPages.RemoveAt(browserTabControl.SelectedIndex);
            }

        }
        //selected index changed
        private void browserTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (browserTabControl.SelectedIndex == browserTabControl.TabPages.Count - 1) addNewTab();
            else
            {
                if (getCurrentBrowser().Url != null)
                    adrBarTextBox.Text = getCurrentBrowser().Url.ToString();
                else adrBarTextBox.Text = "about:blank";

                if (getCurrentBrowser().CanGoBack) toolStripButton1.Enabled = true;
                else toolStripButton1.Enabled = false;

                if (getCurrentBrowser().CanGoForward) toolStripButton2.Enabled = true;
                else toolStripButton2.Enabled = false;
            }
        }

        /* tab context menu */

        private void closeTabToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            closeTab();
        }
        private void duplicateTabToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                Uri dup_url = getCurrentBrowser().Url;
                addNewTab();
                getCurrentBrowser().Url = dup_url;

            }
            else addNewTab();
        }
        #endregion

        #region FAVICON
       
        // favicon
        public static Image favicon(String u, string file)
        {
                Uri url = new Uri(u);
                String iconurl = "http://" + url.Host + "/favicon.ico";

                WebRequest request = WebRequest.Create(iconurl);
                try
                {
                    WebResponse response = request.GetResponse();

                    Stream s = response.GetResponseStream();
                    return Image.FromStream(s);
                }
                catch (Exception ex)
                {
                    return Image.FromFile(file);
                }
            
           
        }
        //favicon index
        private int faviconIndex(string url)
        {
            Uri key = new Uri(url);
            if (!imgList.Images.ContainsKey(key.Host.ToString()))
                imgList.Images.Add(key.Host.ToString(), favicon(url, "link.png"));
            return imgList.Images.IndexOfKey(key.Host.ToString());
        }
        //getFavicon from key
        private Image getFavicon(string key)
        {
            Uri url = new Uri(key);
            if (!imgList.Images.ContainsKey(url.Host.ToString()))
                imgList.Images.Add(url.Host.ToString(), favicon(key
                    , "link.png"));
            return imgList.Images[url.Host.ToString()];
        }
        #endregion

        #region     TOOL CONTEXT MENU
        /* TOOL CONTEXT MENU*/

        //link bar
        private void linksBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            linkBar.Visible = !linkBar.Visible;
            this.linksBarToolStripMenuItem.Checked = linkBar.Visible;
            settings.DocumentElement.ChildNodes[2].Attributes[0].Value = linkBar.Visible.ToString();
        }
        //menu bar
        private void menuBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            menuBar.Visible = !menuBar.Visible;
            this.menuBarToolStripMenuItem.Checked = menuBar.Visible;
            settings.DocumentElement.ChildNodes[0].Attributes[0].Value = menuBar.Visible.ToString();
        }
        //address bar
        private void commandBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adrBar.Visible = !adrBar.Visible;
            this.commandBarToolStripMenuItem.Checked = adrBar.Visible;
            settings.DocumentElement.ChildNodes[1].Attributes[0].Value = adrBar.Visible.ToString();
        }
        #endregion

        #region ADDRESS BAR
        /*ADDRESS BAR*/

        private WebBrowser getCurrentBrowser()
        {
            return (WebBrowser)browserTabControl.SelectedTab.Controls[0];
        }
        //ENTER
        private void adrBarTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //getCurrentBrowser().Navigate(adrBarTextBox.Text);
                string asd = adrBarTextBox.Text;
                    
                Navi(adrBarTextBox.Text);
                adrBarTextBox.Text = asd;
                //getCurrentBrowser().Refresh();
                //getCurrentBrowser().Navigate(adrBarTextBox.Text);
            }
        }
        //select all from adr bar
        private void adrBarTextBox_Click(object sender, EventArgs e)
        {
            adrBarTextBox.SelectAll();
        }
        //show urls

        private void showUrl()
        {
            if (File.Exists(historyXml))
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(historyXml);
                int i = 0;
                int num=int.Parse(settings.DocumentElement.ChildNodes[6].InnerText.ToString());
                foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
                {
                    if (num <= i++ ) break;
                    else  adrBarTextBox.Items.Add(el.GetAttribute("url").ToString());
                           
                }
            }
        }

        private void adrBarTextBox_DropDown(object sender, EventArgs e)
        {
            adrBarTextBox.Items.Clear();
            showUrl();
        }
        //navigate on selected url 
        private void adrBarTextBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate(adrBarTextBox.SelectedItem.ToString());
            Navi(adrBarTextBox.SelectedItem.ToString());
        }
     //canGoForwardChanged
        void browser_CanGoForwardChanged(object sender, EventArgs e)
        {
            toolStripButton2.Enabled = !toolStripButton2.Enabled;
        }
        //canGoBackChanged
        void browser_CanGoBackChanged(object sender, EventArgs e)
        {
            toolStripButton1.Enabled = !toolStripButton1.Enabled;
        }
        //back  
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoBack();
        }
        //forward
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoForward();
        }
        //go
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
          //  getCurrentBrowser().Navigate(adrBarTextBox.Text);
            Navi(adrBarTextBox.Text);
        }
        //refresh
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Refresh();
        }
        //stop
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Stop();
        }
        //favorits
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            favoritesPanel.Visible = !favoritesPanel.Visible;
            settings.DocumentElement.ChildNodes[3].Attributes[0].Value = favoritesPanel.Visible.ToString();
        }
        //add to favorits
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                AddFavorites dlg = new AddFavorites(getCurrentBrowser().Url.ToString());
                DialogResult res = dlg.ShowDialog();

                if (res == DialogResult.OK)
                {
                    if (dlg.favFile == "Favorites")
                        addFavorit(getCurrentBrowser().Url.ToString(), dlg.favName);
                    else addLink(getCurrentBrowser().Url.ToString(), dlg.favName);
                }
                dlg.Close();
            }

        }
        //search
        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                if (googleSearch.Checked == true)
                {
                    //getCurrentBrowser().Navigate("http://google.com/search?q=" + searchTextBox.Text);\
                    Navi("http://google.com/search?q=" + searchTextBox.Text);
                }
                else
                {
                    //getCurrentBrowser().Navigate("http://search.live.com/results.aspx?q=" + searchTextBox.Text);
                    Navi("http://search.live.com/results.aspx?q=" + searchTextBox.Text);
                }
        }

        private void googleSearch_Click(object sender, EventArgs e)
        {
            liveSearch.Checked =!googleSearch.Checked;
        }

        private void liveSearch_Click(object sender, EventArgs e)
        {
            googleSearch.Checked = !liveSearch.Checked;
        }

        #endregion

        #region LINKS BAR

        /*LINKS BAR*/

        string adress, name;

        //favorits button
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            favoritesPanel.Visible = !favoritesPanel.Visible;
            settings.DocumentElement.ChildNodes[3].Attributes[0].Value = favoritesPanel.Visible.ToString();
        }
        //add to favorits bar button
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
                addLink(getCurrentBrowser().Url.ToString(), getCurrentBrowser().Url.ToString());
        }

        //showLinks on link bar
        private void showLinks()
        {
            if (File.Exists(linksXml))
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(linksXml);
                XmlElement root = myXml.DocumentElement;
                foreach (XmlElement el in root.ChildNodes)
                {
                    ToolStripButton b =
                        new ToolStripButton(el.InnerText, getFavicon(el.GetAttribute("url")), items_Click, el.GetAttribute("url"));

                    b.ToolTipText = el.GetAttribute("url");
                    b.MouseUp += new MouseEventHandler(b_MouseUp);
                    linkBar.Items.Add(b);
                }
            }
        }
        //click link button
        private void items_Click(object sender, EventArgs e)
        {
            ToolStripButton b = (ToolStripButton)sender;
            //getCurrentBrowser().Navigate(b.ToolTipText);
            Navi(b.ToolTipText);
        }
        //show context menu on button
        private void b_MouseUp(object sender, MouseEventArgs e)
        {
            ToolStripButton b = (ToolStripButton)sender;
            adress = b.ToolTipText;
            name = b.Text;

            if (e.Button == MouseButtons.Right)
                linkContextMenu.Show(MousePosition);
        }
//visible change
        private void linkBar_VisibleChanged(object sender, EventArgs e)
        {
            if (linkBar.Visible == true) showLinks();
            else while (linkBar.Items.Count > 3) linkBar.Items[linkBar.Items.Count - 1].Dispose();
        }

        #endregion

        #region LINK, FAVORITES, HISTORY CONTEXT MENU
        /*GENERAL*/

        //open
        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate(adress);
            Navi(adress);
        }
        //open in new tab
        private void openInNewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addNewTab();
            //getCurrentBrowser().Navigate(adress);
            Navi(adress);
        }
        //open in new window
        private void openInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WBrowser new_form = new WBrowser();
            new_form.Show();
            //new_form.getCurrentBrowser().Navigate(adress);
            new_form.Navi(adress);
        }
                     /*LINK CONTEXT MENU*/
        //delete link
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteLink();
        }
        //rename link
        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renameLink();
        }
                          /*FAVORITES CONTEXT MENU*/
        //delete favorit
        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            deleteFavorit();
        }
        //rename favorit
        private void renameToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            renameFavorit();
        }
           
              /*HISTORY CONTEXT MENU */

        private void openToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(historyTreeView.SelectedNode.Text);
            Navi(historyTreeView.SelectedNode.Text);
        }

//delete history
        private void deleteToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            deleteHistory();
        }
 //add to favorites
        private void addToFavoritesToolStripMenuItem_Click(object sender, EventArgs e)
        {   
            AddFavorites dlg = new AddFavorites(historyTreeView.SelectedNode.Text);
             DialogResult res = dlg.ShowDialog();
                if (res == DialogResult.OK)
                {
                    if (dlg.favFile == "Favorites")
                        addFavorit(getCurrentBrowser().Url.ToString(), dlg.favName);
                    else addLink(getCurrentBrowser().Url.ToString(), dlg.favName);
                   
                    deleteHistory();
                }
                dlg.Close();

                
        }

        #endregion

        #region FAVORITES WINDOW

        private void showFavorites()
        {
            XmlDocument myXml = new XmlDocument();
            TreeNode link = new TreeNode("Links",0,0);
            link.NodeFont =new  Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            favTreeView.Nodes.Add(link);

            if (File.Exists(favXml))
            {
                myXml.Load(favXml);

                foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
                {
                    TreeNode node = 
                        new TreeNode(el.InnerText,faviconIndex(el.GetAttribute("url")), faviconIndex(el.GetAttribute("url")));
                    node.ToolTipText = el.GetAttribute("url");
                    node.Name = el.GetAttribute("url");
                    node.ContextMenuStrip = favContextMenu;
                    favTreeView.Nodes.Add(node);
                }

            }

            if (File.Exists(linksXml))
            {
                myXml.Load(linksXml);

                foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
                {
                    TreeNode node = 
                        new TreeNode(el.InnerText, faviconIndex(el.GetAttribute("url")), faviconIndex(el.GetAttribute("url")));
                    node.ToolTipText = el.GetAttribute("url");
                    node.Name = el.GetAttribute("url");
                    node.ContextMenuStrip = linkContextMenu;
                    favTreeView.Nodes[0].Nodes.Add(node);
                }

            }

        }
//node click
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                favTreeView.SelectedNode = e.Node;
                adress = e.Node.ToolTipText;
                name = e.Node.Text;
            }
            else
                if (e.Node != favTreeView.Nodes[0])
                {
                 //   getCurrentBrowser().Navigate(e.Node.ToolTipText);
                    Navi(e.Node.ToolTipText);
                }

        }
//show history in tree wiew
        private void showHistory()
        {
            historyTreeView.Nodes.Clear();
            XmlDocument myXml = new XmlDocument();

            if (File.Exists(historyXml))
            {
                myXml.Load(historyXml);
                DateTime now=DateTime.Now;
                if (comboBox1.Text.Equals("Ordered Visited Today"))
                {
                    historyTreeView.ShowRootLines = false;
                    foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
                    {
                         DateTime d=DateTime.Parse(el.GetAttribute("lastVisited"),currentCulture);
                        
                        if (!(d.Date==now.Date)) return;

                        TreeNode node =
                            new TreeNode(el.GetAttribute("url"), 3, 3);
                        node.ToolTipText = el.GetAttribute("url") + "\nLast Visited: " + el.GetAttribute("lastVisited") + "\nTimes Visited: " + el.GetAttribute("times");
                        node.Name = el.GetAttribute("url");
                        node.ContextMenuStrip = histContextMenu;
                        historyTreeView.Nodes.Add(node);
                    }

                }
               
           if (comboBox1.Text.Equals("View By Site"))
           {
              historyTreeView.ShowRootLines = true;
              foreach(XmlElement el in myXml.DocumentElement.ChildNodes)
              { Uri site=new Uri(el.GetAttribute("url"));
                  
                  if(!historyTreeView.Nodes.ContainsKey(site.Host.ToString()))
                      historyTreeView.Nodes.Add(site.Host.ToString(),site.Host.ToString(),0,0);
                   TreeNode node =new TreeNode(el.GetAttribute("url"), 3, 3);
                    node.ToolTipText = el.GetAttribute("url") + "\nLast Visited: " + el.GetAttribute("lastVisited") + "\nTimes Visited: " + el.GetAttribute("times");
                    node.Name = el.GetAttribute("url");
                    node.ContextMenuStrip = histContextMenu;
                    historyTreeView.Nodes[site.Host.ToString()].Nodes.Add(node);
               }

           }

           if (comboBox1.Text.Equals("View by Date"))
           {  
               historyTreeView.ShowRootLines = true;
               historyTreeView.Nodes.Add("2 Weeks Ago","2 Weeks Ago",2,2);
               historyTreeView.Nodes.Add("Last Week","Last Week",2,2);
               historyTreeView.Nodes.Add("This Week","This Week",2,2);
               historyTreeView.Nodes.Add("Yesterday","Yesterday",2,2);
               historyTreeView.Nodes.Add("Today","Today",2,2);
               foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
               {
                  DateTime d=DateTime.Parse(el.GetAttribute("lastVisited"),currentCulture);
                  
                   TreeNode node = new TreeNode(el.GetAttribute("url"), 3, 3);
                   node.ToolTipText = el.GetAttribute("url") + "\nLast Visited: " + el.GetAttribute("lastVisited") + "\nTimes Visited: " + el.GetAttribute("times");
                   node.Name = el.GetAttribute("url");
                   node.ContextMenuStrip = histContextMenu;

                   if (d.Date==now.Date)
                       historyTreeView.Nodes[4].Nodes.Add(node);
                   else
                       if (d.AddDays(1).ToShortDateString().Equals(now.ToShortDateString()))
                           historyTreeView.Nodes[3].Nodes.Add(node);
                       else
                           if (d.AddDays(7) > now)
                               historyTreeView.Nodes[2].Nodes.Add(node);
                           else
                               if (d.AddDays(14) > now)
                                   historyTreeView.Nodes[1].Nodes.Add(node);
                               else
                                   if (d.AddDays(21) > now)
                                       historyTreeView.Nodes[0].Nodes.Add(node);
                                      else
                                       if (d.AddDays(22) > now)
                                           myXml.DocumentElement.RemoveChild(el);
               }
           }
         }
            
            
        }
//history nodes click
        private void historyTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                historyTreeView.SelectedNode = e.Node;
                adress = e.Node.Text;
            }
            else
                if (!comboBox1.Text.Equals("Ordered Visited Today"))
                {
                    if (!historyTreeView.Nodes.Contains(e.Node))
                    {
                        //getCurrentBrowser().Navigate(e.Node.Text);
                        Navi(e.Node.Text);
                    }
                }
                else
                {
                    //getCurrentBrowser().Navigate(e.Node.Text);
                    Navi(e.Node.Text);
                }
        }

//fav panel visible change
        private void favoritesPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (favoritesPanel.Visible == true)
            {
                showFavorites();
                showHistory();
            }
            else
            {
                favTreeView.Nodes.Clear();
                historyTreeView.Nodes.Clear();
            }
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            showHistory();
        }

        #endregion

        #region FAVORITS
        /*FAVORITES*/

        //add to favorits
        private void addToFavoritsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                AddFavorites dlg = new AddFavorites(getCurrentBrowser().Url.ToString());
                DialogResult res = dlg.ShowDialog();

                if (res == DialogResult.OK)
                {
                    if (dlg.favFile == "Favorites")
                        addFavorit(getCurrentBrowser().Url.ToString(), dlg.favName);
                    else addLink(getCurrentBrowser().Url.ToString(), dlg.favName);
                }
                dlg.Close();
            }
        }
        //add to favorits bar
        private void addToFavoritsBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addLink(getCurrentBrowser().Url.ToString(), getCurrentBrowser().Url.ToString());
        }
        //organize favorites
        private void organizeFavoritsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new OrganizeFavorites(favTreeView, linkBar,linkContextMenu,favContextMenu)).ShowDialog();
        }

        //show favorites in menu
        private void favoritesToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            XmlDocument myXml = new XmlDocument();
            myXml.Load(favXml);

            for (int i = favoritesToolStripMenuItem.DropDownItems.Count - 1; i > 5; i--)
            {
                favoritesToolStripMenuItem.DropDownItems.RemoveAt(i);
            }
            foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(el.InnerText, getFavicon(el.GetAttribute("url")), fav_Click);
                item.ToolTipText = el.GetAttribute("url");
                favoritesToolStripMenuItem.DropDownItems.Add(item);
            }
        }
        //show links in menu
        private void linksMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            XmlDocument myXml = new XmlDocument();
            myXml.Load(linksXml);
            linksMenuItem.DropDownItems.Clear();
            foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
            {   
                ToolStripMenuItem item = new ToolStripMenuItem(el.InnerText,getFavicon(el.GetAttribute("url")), fav_Click);
                item.ToolTipText = el.GetAttribute("url");
                linksMenuItem.DropDownItems.Add(item);
            }
        }
        private void fav_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem m = (ToolStripMenuItem)sender;
            //getCurrentBrowser().Navigate(m.ToolTipText);
            Navi(m.ToolTipText);
        }
        #endregion

        #region FILE
        /*FILE*/

        //new tab
        private void newTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addNewTab();
        }
        //duplicate tab
        private void duplicateTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                Uri dup_url = getCurrentBrowser().Url;
                addNewTab();
                getCurrentBrowser().Url = dup_url;

            }
            else addNewTab();
        }
        //new window
        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new WBrowser()).Show();

        }
        //close tab
        private void closeTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeTab();
        }
        //open
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Open(getCurrentBrowser())).Show();
        }
        //page setup
        private void pageSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPageSetupDialog();
        }
        //save as
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowSaveAsDialog();
        }
        //print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPrintDialog();

        }
        //print preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPrintPreviewDialog();
        }
        //properties
        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPropertiesDialog();
        }
        //send page by email
        private void pageByEmailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate("https://login.yahoo.com/config/login_verify2?&.src=ym");
            Process.Start("msimn.exe");
        }
        //send link by email
        private void linkByEmailToolStripMenuItem_Click(object sender, EventArgs e)
        {
           // getCurrentBrowser().Navigate("https://login.yahoo.com/config/login_verify2?&.src=ym");
            Process.Start("msimn.exe");
        }


        #endregion

        #region EDIT
        /*EDIT*/
        //cut
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Cut", false, null);

        }
        //copy
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Copy", false, null);

        }
        //paste
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Paste", false, null);
        }
        //select all
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("SelectAll", true, null);
        }
        #endregion

        #region VIEW
       
        /* VIEW */

//explorer bars
        private void favoritsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            favoritesPanel.Visible = true;
            favoritesTabControl.SelectedTab = favTabPage;

        }

        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            favoritesPanel.Visible = true;
            favoritesTabControl.SelectedTab = historyTabPage;
        }
//favorites,history checked
        private void explorerBarsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            favoritesViewMenuItem.Checked =
                (favoritesPanel.Visible == true && favoritesTabControl.SelectedTab == favTabPage);

            historyViewMenuItem.Checked =
                (favoritesPanel.Visible == true && favoritesTabControl.SelectedTab == historyTabPage);
        }

        /*Go to*/
//drop down opening
        private void goToToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            backToolStripMenuItem.Enabled = getCurrentBrowser().CanGoBack;
            forwardToolStripMenuItem.Enabled = getCurrentBrowser().CanGoForward;

            while (goToMenuItem.DropDownItems.Count > 5)
                goToMenuItem.DropDownItems.RemoveAt(goToMenuItem.DropDownItems.Count-1);
            
            foreach (string a in urls)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(a, null, goto_click);

                item.Checked = (getCurrentBrowser().Url.Host.ToString().Equals(a));

                goToMenuItem.DropDownItems.Add(item);
            }
        }
        private void goto_click(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate(sender.ToString());
            Navi(sender.ToString());
        }
        //back
        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoBack();
        }
        //forward
        private void forwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoForward();
        }
        //home
        private void homePageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate(homePage);
            Navi(homePage);
        }
                    /*Stop*/
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Stop();
        }
                    /*Refresh*/
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Refresh();
        }
                     /*view source*/
        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String source=("source.txt");
            StreamWriter writer =File.CreateText(source);
            writer.Write(getCurrentBrowser().DocumentText);
            writer.Close();
            Process.Start("notepad.exe", source);            
        }
        //text size 
        private void textSizeToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string level = e.ClickedItem.ToString();
            smallerToolStripMenuItem.Checked = false;
            smallestToolStripMenuItem.Checked = false;
            mediumToolStripMenuItem.Checked = false;
            largerToolStripMenuItem.Checked = false;
            largestToolStripMenuItem.Checked = false;
            switch (level)
            {
                case "Smallest": getCurrentBrowser().Document.ExecCommand("FontSize", true, "0");
                                 smallestToolStripMenuItem.Checked = true;
                                 break;
                case "Smaller": getCurrentBrowser().Document.ExecCommand("FontSize", true, "1");
                                 smallerToolStripMenuItem.Checked = true;
                                 break;
                case "Medium": getCurrentBrowser().Document.ExecCommand("FontSize",true,"2");
                                 mediumToolStripMenuItem.Checked = true; 
                                break;
                case "Larger": getCurrentBrowser().Document.ExecCommand("FontSize",true,"3");
                                largerToolStripMenuItem.Checked = true; 
                                break;
                case "Largest": getCurrentBrowser().Document.ExecCommand("FontSize",true,"4");
                                largestToolStripMenuItem.Checked = true;
                                 break;
            }
        }
        //full screen
        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!(this.FormBorderStyle == FormBorderStyle.None && this.WindowState == FormWindowState.Maximized))
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
                menuBar.Visible = false;
                linkBar.Visible = false;
                adrBar.Visible = false;
                favoritesPanel.Visible = false;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.TopMost = false;
                menuBar.Visible = (settings.DocumentElement.ChildNodes[0].Attributes[0].Value.Equals("True"));
                adrBar.Visible = (settings.DocumentElement.ChildNodes[1].Attributes[0].Value.Equals("True"));
                linkBar.Visible = (settings.DocumentElement.ChildNodes[2].Attributes[0].Value.Equals("True"));
                favoritesPanel.Visible = (settings.DocumentElement.ChildNodes[3].Attributes[0].Value.Equals("True"));
            }
        }
        //splash screen
        private void splashScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DocumentElement.ChildNodes[4].Attributes[0].Value
                = splashScreenToolStripMenuItem.Checked.ToString();
        }

        #endregion

        #region TOOLS

//delete browsing history
        private void deleteBrowserHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteBrowsingHistory b = new DeleteBrowsingHistory();
            if (b.ShowDialog() == DialogResult.OK)
            {
                if (b.History.Checked == true)
                {
                    File.Delete(historyXml);
                    historyTreeView.Nodes.Clear();
                }
                if (b.TempFiles.Checked == true)
                {
                    urls.Clear();
                    while (imgList.Images.Count > 4)
                        imgList.Images.RemoveAt(imgList.Images.Count-1);
                    File.Delete("source.txt");

                }
            }
        }
//internet options
        private void internetOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InternetOption intOp = new InternetOption(getCurrentBrowser().Url.ToString());
            if (intOp.ShowDialog() == DialogResult.OK)
            {
                if (!intOp.homepage.Text.Equals(""))
                {
                    homePage = intOp.homepage.Text;
                    settings.DocumentElement.ChildNodes[5].InnerText = intOp.homepage.Text;
                }
                    if (intOp.deleteHistory.Checked == true)
                {
                    File.Delete(historyXml);
                    historyTreeView.Nodes.Clear();
                }
                settings.DocumentElement.ChildNodes[6].InnerText = intOp.num.Value.ToString();
                ActiveForm.ForeColor = intOp.forecolor;
                ActiveForm.BackColor = intOp.backcolor;
                linkBar.BackColor = intOp.backcolor;
                adrBar.BackColor = intOp.backcolor;
                ActiveForm.Font = intOp.font;
                linkBar.Font = intOp.font;
                menuBar.Font = intOp.font;
            }


        }

//calculator
        private void yahooMessengerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("calc.exe");
        }
        //calendar
        private void calendarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Calendar()).Show();
        }
        //solitaire
        private void solitaireToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("sol.exe");
        }
        private void yahooMailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //getCurrentBrowser().Navigate("https://login.yahoo.com/config/login_verify2?&.src=ym");
            Navi("https://login.yahoo.com/config/login_verify2?&.src=ym");
        }

        #endregion

        #region HELP
        //about
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new About(false)).Show();
        }
       private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start("mailto:goga.claudia@gmail.com");
        }
 #endregion         
       

       public Boolean checkURL(string url1)
       {
           string url;
           if (url1.StartsWith("http://") || url1.StartsWith("https://"))
               url = url1 ;
           else
               url = "http://" + url1;


           
           string title = "";
           try
           {
               HttpWebRequest request = (HttpWebRequest.Create(url) as HttpWebRequest);
               HttpWebResponse response = (request.GetResponse() as HttpWebResponse);

               using (Stream stream = response.GetResponseStream())
               {
                   // compiled regex to check for <title></title> block
                   Regex titleCheck = new Regex(@"<title>\s*(.+?)\s*</title>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                   int bytesToRead = 8092;
                   byte[] buffer = new byte[bytesToRead];
                   string contents = "";
                   int length = 0;
                   while ((length = stream.Read(buffer, 0, bytesToRead)) > 0)
                   {
                       // convert the byte-array to a string and add it to the rest of the
                       // contents that have been downloaded so far
                       contents += Encoding.UTF8.GetString(buffer, 0, length);

                       Match m = titleCheck.Match(contents);
                       if (m.Success)
                       {
                           // we found a <title></title> match =]
                           title = m.Groups[1].Value.ToString();
                           break;
                       }
                       else if (contents.Contains("</head>"))
                       {
                           //title = "Access Denied";
                           // reached end of head-block; no title found =[
                           break;
                       }
                   }
               }

               if (title == "Access Denied")
                   return false;
               else
                   return true;
        

           }
           catch (Exception e)
           {
               if (title == "Access Denied")
                   return false;
               else
                   return true;
               //Console.WriteLine(e);
           }
       }


       Hashtable ht = new Hashtable();

       int flag123 = 0;
       int flag567 = 0;
       public void Navi(string url1)
       {


           ///////////////////////////////////////////////////////////////
           if (flag123 == 1)
           {
               MessageBox.Show("Please wait a Moment , while we switch your Primary connection.");
               string chk = findprimarychecked();
               //MessageBox.Show("cheched now is :" + chk);
               string pip = ipofprimary(chk);
               //MessageBox.Show(pip);
               Process p = new Process();
               p.StartInfo.UseShellExecute = false;
               p.StartInfo.CreateNoWindow = true;
               p.StartInfo.FileName = "route";
               p.StartInfo.Arguments = "delete 0.0.0.0";
               p.StartInfo.RedirectStandardOutput = false;
               //p.StartInfo.StandardOutputEncoding = Encoding.ASCII;
               p.Start();
               p.WaitForExit();

               Process p1 = new Process();
               p1.StartInfo.UseShellExecute = false;
               p1.StartInfo.CreateNoWindow = true;
               p1.StartInfo.FileName = "route";
               p1.StartInfo.Arguments = "add 0.0.0.0" + " mask 0.0.0.0 " + pip + " metric 10";
               p1.StartInfo.RedirectStandardOutput = false;
               //p.StartInfo.StandardOutputEncoding = Encoding.ASCII;
               p1.Start();
               p1.WaitForExit();

               //MessageBox.Show("done adding route for primary connection");


               flag123 = 0;
           }



           if (flag567 == 1)
           {


               flag567 = 0;
           }











           /////////////////////////////////////////////////////////////////
           string url;
           if (url1.StartsWith("http://") || url1.StartsWith("https://"))
               url = url1;
           else
               url = "http://" + url1;


           if (!checkURL(url1) && status)
           {
              // string stackoverflow = "http://www.google.com";
               string s = url;
             //  string s = adrBarTextBox.Text;
             //  int i = s.IndexOf(':');
             //  if (i > 0) s = s.Substring(i + 3);


               string Url =s;
               if (!Url.Contains("://"))
                   Url = "http://" + Url;

               //Console.WriteLine(new Uri(Url).Host);
               s = new Uri(Url).Host;
               
               
               //Console.WriteLine(s);
               //  Console.WriteLine(Dns.GetHostAddresses(s));
               toolStripLabel1.Text = "Connecting Through : "+findsecondarychecked();
               IPAddress[] addresslist = Dns.GetHostAddresses(s);

               foreach (IPAddress theaddress in addresslist)
               {
                   Console.WriteLine(theaddress.ToString());
                   string ip = theaddress.ToString();
                   string secchk = findsecondarychecked();

                   string ip1 = ipofsecondary(secchk);
                   Process p = new Process();

                   p.StartInfo.UseShellExecute = false;
                   p.StartInfo.CreateNoWindow = true;


                   p.StartInfo.FileName = "route";

                   p.StartInfo.Arguments = "add " + ip + " mask 255.255.255.255 " + ip1;

                   p.StartInfo.RedirectStandardOutput = false;

                   //p.StartInfo.StandardOutputEncoding = Encoding.ASCII;

                    p.Start();
                   p.WaitForExit();
                   //Console.WriteLine("Route added for " + ip);
                  // MessageBox.Show("route added");
                    if (!ht.ContainsKey(ip))
                    { ht.Add(ip, url); }
               }


               getCurrentBrowser().Navigate(s);
               //getCurrentBrowser().Refresh();
               //toolStripButton4_Click(object sender, EventArgs e);
           }


           else
           {
               string wert=findprimarychecked();
               if (findprimarychecked() == "")
                   wert = "Default";
            //   toolStripLabel1.Text = "Connecting Through : "+wert;
               toolStripLabel1.Text = "Connecting Through : TATA PHOTON+";
               getCurrentBrowser().Navigate(url1);
           }
               

       }




        //switch menu

       Boolean status = false;
       private void enableSwitchToolStripMenuItem_Click(object sender, EventArgs e)
       {
           //menuBar.Visible = !menuBar.Visible;

           status = !status;
           this.enableSwitchToolStripMenuItem.Checked = status;


           if (!status)
           {
               secondaryConnectionToolStripMenuItem.Enabled = false;
               primaryConnectionToolStripMenuItem.Enabled = true;
               clearroute();
               MessageBox.Show("All Routes Deleted.");
           }
           else
           {
               //MessageBox.Show("Please Select a Secondary Connection");
               secondaryConnectionToolStripMenuItem.DropDownItems.Clear();
               secondaryConnectionToolStripMenuItem.Enabled = true;
               primaryConnectionToolStripMenuItem.Enabled = false;
               string cvb = findprimarychecked();
               foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
               {

                   if (netif.OperationalStatus == OperationalStatus.Up && netif.Name != cvb )
                   {
                       ToolStripMenuItem itempc = new ToolStripMenuItem();
                       string name = netif.Name.ToString();
                       itempc.Name = (name);
                       itempc.Text = (name);
                       itempc.CheckOnClick = true;
                       //MessageBox.Show("done");
                       secondaryConnectionToolStripMenuItem.DropDownItems.Add(itempc);
                       if (name.StartsWith("TATA"))
                       {
                           itempc.Checked = true;
                       }

                   }

               }




           }

           //settings.DocumentElement.ChildNodes[0].Attributes[0].Value = menuBar.Visible.ToString();

       }

       private void clearRoutesToolStripMenuItem_Click(object sender, EventArgs e)
       {
           
           clearroute();
           MessageBox.Show("All Routes Deleted.");
       }
        
    
        
       public void clearroute()
       {
           try
           {


               foreach (DictionaryEntry gg in ht)
               {

                   Process p = new Process();
                   p.StartInfo.UseShellExecute = false;
                   p.StartInfo.CreateNoWindow = true;
                   p.StartInfo.FileName = "route";
                   p.StartInfo.Arguments = "delete " + gg.Key;
                   p.StartInfo.RedirectStandardOutput = false;
                   //p.StartInfo.StandardOutputEncoding = Encoding.ASCII;
                   p.Start();
                   p.WaitForExit();



               }

               foreach (DictionaryEntry gg in ht)
               {
                   ht.Remove(gg.Key);

               }

           }
           catch (Exception ex)
           { }

       }
     
       private void primaryConnectionToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
       {
           //MessageBox.Show("Inside primary dropdown clicked event");
           //get the parent item
           ToolStripMenuItem ownerItem = e.ClickedItem.OwnerItem as ToolStripMenuItem;
           if (ownerItem != null)
           {
               //uncheck all item
               foreach (ToolStripMenuItem item in ownerItem.DropDownItems)
               {
                   item.Checked = false;
               }
           }
           //it will check the clicked item automatically

           flag123 = 1;

       }

       public string findprimarychecked()
       {
           string check = "";
          
               foreach (ToolStripMenuItem item in primaryConnectionToolStripMenuItem.DropDownItems)
               {
              
                   if (item.Checked)
                   {
                      
                       check = item.Name;
                       break;
                   }
               }

           //}
               //MessageBox.Show(check);
           return check;
 
       }

       public string findsecondarychecked()
       {
           string check = "";
           
           foreach (ToolStripMenuItem item in secondaryConnectionToolStripMenuItem.DropDownItems)
           {
               // MessageBox.Show(item.CheckState + "   " + item.Name);
               if (item.Checked)
               {
                   // MessageBox.Show(item.Name + "   " + item.Text);
                   check = item.Name;
                   break;
               }
           }

           //}
           //MessageBox.Show(check);
           return check;
 
       }

       public string ipofprimary(string nicname)
       {
           string ip="";
           foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
           {
              
               if (netif.Name == nicname)
               {
                   IPInterfaceProperties properties = netif.GetIPProperties();
                   foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                       ip= Convert.ToString(unicast.Address);
                   //MessageBox.Show("ip got of primary");
 
               }
              
           }
           return ip;
       }

       public string ipofsecondary(string nicname)
       {
           string ip = "";
           foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
           {

               if (netif.Name == nicname)
               {
                   IPInterfaceProperties properties = netif.GetIPProperties();
                   foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                       ip = Convert.ToString(unicast.Address);
                   //MessageBox.Show("ip got of primary");

               }

           }
           return ip;
 
       }

       private void secondaryConnectionToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
       {
           //MessageBox.Show("Inside primary dropdown clicked event");
           //get the parent item
           ToolStripMenuItem ownerItem = e.ClickedItem.OwnerItem as ToolStripMenuItem;
           if (ownerItem != null)
           {
               //uncheck all item
               foreach (ToolStripMenuItem item in ownerItem.DropDownItems)
               {
                   item.Checked = false;
               }
           }
           //it will check the clicked item automatically

           flag567 = 1;
       }

     


    }
}
