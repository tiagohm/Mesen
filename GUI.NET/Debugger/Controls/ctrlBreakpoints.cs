﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mesen.GUI.Debugger.Controls
{
	public partial class ctrlBreakpoints : UserControl
	{
		public event EventHandler BreakpointChanged;
		private List<Breakpoint> _breakpoints = new List<Breakpoint>();

		public ctrlBreakpoints()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			AdjustColumnWidth();
		}

		public void ToggleBreakpoint(int address, bool toggleEnabled)
		{
			if(address >= 0) {
				Breakpoint breakpoint = GetMatchingBreakpoint(address);
				if(breakpoint != null) {
					if(toggleEnabled) {
						breakpoint.Enabled = !breakpoint.Enabled;
					} else {
						_breakpoints.Remove(breakpoint);
					}
				} else {
					breakpoint = new Breakpoint() {
						BreakOnExec = true,
						Address = (UInt32)address,
						IsAbsoluteAddress = false,
						Enabled = true
					};
					_breakpoints.Add(breakpoint);
				}
				RefreshList();
				OnBreakpointChanged();
			}
		}

		private Breakpoint GetMatchingBreakpoint(int address)
		{
			foreach(Breakpoint breakpoint in _breakpoints) {
				if(breakpoint.Address == address) {
					return breakpoint;
				}
			}
			return null;
		}

		public List<Breakpoint> GetBreakpoints()
		{
			return _breakpoints;
		}

		public void SetBreakpoints()
		{
			List<InteropBreakpoint> breakpoints = new List<InteropBreakpoint>();
			foreach(Breakpoint bp in GetBreakpoints()) {
				if(bp.Enabled) {
					breakpoints.Add(bp.ToInteropBreakpoint());
				}
			}
			InteropEmu.DebugSetBreakpoints(breakpoints.ToArray(), (UInt32)breakpoints.Count);
		}

		private void RefreshList()
		{
			lstBreakpoints.ItemChecked -= new System.Windows.Forms.ItemCheckedEventHandler(lstBreakpoints_ItemChecked);
			lstBreakpoints.Items.Clear();
			foreach(Breakpoint breakpoint in _breakpoints) {
				string address = "$" + breakpoint.Address.ToString("X");
				if(breakpoint.IsAbsoluteAddress) {
					address = "[" + address + "]";
				}

				ListViewItem item = new ListViewItem();
				item.Tag = breakpoint;
				item.Checked = breakpoint.Enabled;
				item.SubItems.Add(breakpoint.Type.ToString());
				item.SubItems.Add(breakpoint.SpecificAddress ? address : "<any>");
				item.SubItems.Add(breakpoint.Condition);
				lstBreakpoints.Items.Add(item);
			}
			lstBreakpoints.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(lstBreakpoints_ItemChecked);

			SetBreakpoints();
		}

		private void lstBreakpoints_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			if(e.ColumnIndex == 2) {
				e.Cancel = true;
			}
			AdjustColumnWidth();
		}

		private void lstBreakpoints_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			AdjustColumnWidth();
		}

		private void AdjustColumnWidth()
		{
			lstBreakpoints.ColumnWidthChanging -= lstBreakpoints_ColumnWidthChanging;
			lstBreakpoints.ColumnWidthChanged -= lstBreakpoints_ColumnWidthChanged;

			//Force watch values to take the full width of the list
			int totalWidth = lstBreakpoints.Columns[0].Width + lstBreakpoints.Columns[1].Width + lstBreakpoints.Columns[2].Width + lstBreakpoints.Columns[3].Width;
			colLastColumn.Width = lstBreakpoints.ClientSize.Width - totalWidth;

			lstBreakpoints.ColumnWidthChanging += lstBreakpoints_ColumnWidthChanging;
			lstBreakpoints.ColumnWidthChanged += lstBreakpoints_ColumnWidthChanged;
		}

		private void OnBreakpointChanged()
		{
			SetBreakpoints();
			if(BreakpointChanged != null) {
				BreakpointChanged(this, null);
			}
		}

		private void lstBreakpoints_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if(((Breakpoint)e.Item.Tag).Enabled != e.Item.Checked) {
				((Breakpoint)e.Item.Tag).SetEnabled(e.Item.Checked);
				OnBreakpointChanged();
			}
		}

		private void lstBreakpoints_DoubleClick(object sender, EventArgs e)
		{
			if(lstBreakpoints.SelectedItems.Count > 0) {
				new frmBreakpoint(((Breakpoint)lstBreakpoints.SelectedItems[0].Tag)).ShowDialog();
				RefreshList();
				OnBreakpointChanged();
			}
		}

		private void mnuRemoveBreakpoint_Click(object sender, EventArgs e)
		{
			foreach(ListViewItem item in lstBreakpoints.SelectedItems) {
				_breakpoints.Remove((Breakpoint)item.Tag);
			}
			RefreshList();
			OnBreakpointChanged();
		}

		private void mnuAddBreakpoint_Click(object sender, EventArgs e)
		{
			Breakpoint breakpoint = new Breakpoint();
			if(new frmBreakpoint(breakpoint).ShowDialog() == DialogResult.OK) {
				_breakpoints.Add(breakpoint);
				RefreshList();
				OnBreakpointChanged();
			}
		}

		private void contextMenuBreakpoints_Opening(object sender, CancelEventArgs e)
		{
			mnuRemoveBreakpoint.Enabled = (lstBreakpoints.SelectedItems.Count > 0);
		}
	}
}
