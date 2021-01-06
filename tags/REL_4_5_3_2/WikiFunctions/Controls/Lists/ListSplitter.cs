/*
ListComparer
Copyright (C) 2007 Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.IO;

using WikiFunctions.AWBSettings;

namespace WikiFunctions.Controls.Lists
{
    public partial class ListSplitter : Form
    {
        readonly UserPrefs P;
        readonly List<Type> Types;

        public ListSplitter(UserPrefs prefs, List<Type> type)
        {
            InitializeComponent();
            P = prefs;
            Types = type;
        }

        public ListSplitter(UserPrefs prefs, List<Type> type, List<Article> list)
            : this(prefs, type)
        {
            listMaker1.Add(list);
        }

        private void ListSplitter_Load(object sender, EventArgs e)
        {
            listMaker1.MakeListEnabled = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            saveTXT.FileName = listMaker1.SourceText;
            if (saveTXT.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(saveTXT.FileName))
                Save(saveTXT.FileName, false);
        }

        private void btnXMLSave_Click(object sender, EventArgs e)
        {
            saveXML.FileName = listMaker1.SourceText;
            if (saveXML.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(saveXML.FileName))
                Save(saveXML.FileName, true);
        }

        private void Save(string path, bool xml)
        {
            try
            {
                listMaker1.AlphaSortList();
                int noA = listMaker1.Count;

                int roundlimit = Convert.ToInt32(numSplitAmount.Value/2);

                if ((noA%numSplitAmount.Value) <= roundlimit)
                    noA += roundlimit;

                int noGroups =
                    Convert.ToInt32((Math.Round(noA/numSplitAmount.Value)*numSplitAmount.Value)/numSplitAmount.Value);

                if (xml)
                {
                    for (int i = 0; i < noGroups; i++)
                    {
                        List<Article> listart = new List<Article>();
                        for (int j = 0; j < numSplitAmount.Value && listMaker1.Count != 0; j++)
                        {
                            listart.Add(listMaker1.SelectedArticle());
                            listMaker1.Remove(listMaker1.SelectedArticle());
                        }

                        P.List.ArticleList = listart;

                        using (
                            FileStream fStream = new FileStream(path.Replace(".xml", " " + (i + 1) + ".xml"),
                                                                FileMode.Create))
                        {
                            XmlSerializer xs = new XmlSerializer(typeof (UserPrefs), Types.ToArray());
                            xs.Serialize(fStream, P);
                        }
                    }
                    MessageBox.Show("Lists Saved to AWB Settings Files");
                }
                else
                {
                    for (int i = 0; i < noGroups; i++)
                    {
                        StringBuilder strList = new StringBuilder();

                        for (int j = 0; j < numSplitAmount.Value && listMaker1.Count != 0; j++)
                        {
                            strList.AppendLine(listMaker1.SelectedArticle().ToString());
                            listMaker1.Remove(listMaker1.SelectedArticle());
                        }
                        Tools.WriteTextFileAbsolutePath(strList.ToString(), path.Replace(".txt", " " + (i + 1) + ".txt"), false);
                    }
                }
                MessageBox.Show("Lists saved to text files");
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }
    }
}