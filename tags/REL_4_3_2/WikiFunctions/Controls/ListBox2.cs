/*

Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace WikiFunctions.Controls.Lists
{
    public partial class ListBox2 : ListBox, IEnumerable<Article>
    {
        public ListBox2()
        {
            InitializeComponent();
        }

        public IEnumerator<Article> GetEnumerator()
        {
            int i = 0;
            while (i < this.Items.Count)
            {
                yield return (Article)this.Items[i];
                i++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            int i = 0;
            while (i < this.Items.Count)
            {
                yield return (Article)this.Items[i];
                i++;
            }
        }
    }
}
