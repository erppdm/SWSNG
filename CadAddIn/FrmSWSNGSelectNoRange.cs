using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace SWSNG
{
	public partial class FrmSWSNGSelectNoRange : Form
	{
		private static string sqlSpGetNoRanges = @"[dbo].[GetNoRanges]";
		private static string sqlSpGetNextSerialNo = @"[dbo].[GetNextSerialNo]";
		// Change all with the @ prefix with the real values.
		string sqlConnString = @"Persist Security Info=True;User ID=@usr;Password=@pw;Initial Catalog=@dbName; Data Source=@server;Connection Timeout=10";

		public string serialNo { get; private set; }

		public FrmSWSNGSelectNoRange()
		{
			InitializeComponent();
			SelectNoRange();
		}

		private bool SelectNoRange()
		{
			bool ret = false;
			try
			{
				this.lBoxNumberRanges.Items.Clear();
				List<string> nORanges = new List<string>();
				GetNoRanges(ref nORanges);
				if (nORanges == null)
				{ return ret; }
				for (int i = 0; i < nORanges.Count; i++)
				{ this.lBoxNumberRanges.Items.Add(nORanges[i]); }
				ret = true;
			}
			catch (Exception ex)
			{ return ret; }
			return ret;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.serialNo = GetSerialNo(this.lBoxNumberRanges.GetItemText(lBoxNumberRanges.SelectedItem));
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{ this.serialNo = string.Empty; }

		private void GetNoRanges(ref List<string> nORages)
		{
			nORages = null;
			try
			{
				using (SqlConnection connection = new SqlConnection(sqlConnString))
				{
					using (SqlCommand command = new SqlCommand(sqlSpGetNoRanges, connection))
					{
						try
						{ connection.Open(); }
						catch (Exception ex)
						{ return; }
						command.CommandType = CommandType.StoredProcedure;
						SqlParameter errCode = new SqlParameter("@errCode", SqlDbType.Int, int.MaxValue);
						errCode.Direction = ParameterDirection.Output;
						command.Parameters.Add(errCode);
						SqlParameter errMsg = new SqlParameter("@errMsg", SqlDbType.NVarChar, -1);
						errMsg.Direction = ParameterDirection.Output;
						command.Parameters.Add(errMsg);
						command.ExecuteNonQuery();
						if ((int)errCode.Value != 0)
						{
							SWSNG.errCode = (int)errCode.Value;
							SWSNG.errMsg = @"SQL Error: " + (string)errMsg.Value;
							return;
						}
						using (SqlDataReader reader = command.ExecuteReader())
						{
							if (!reader.HasRows)
							{
								SWSNG.errCode = 100;
								SWSNG.errMsg = @"Error reading the number ranges.";
								return;
							}
							nORages = new List<string>();
							while (reader.Read())
							{ nORages.Add((string)reader[0]); }
						}
					}
				}
			}
			catch (Exception ex)
			{
				SWSNG.errCode = 666;
				SWSNG.errMsg = ex.Message;
			}
		}

		private string GetSerialNo(string nORage)
		{
			string retStr = string.Empty;
			try
			{
				using (SqlConnection connection = new SqlConnection(sqlConnString))
				{
					using (SqlCommand command = new SqlCommand(sqlSpGetNextSerialNo, connection))
					{
						try
						{ connection.Open(); }
						catch (Exception ex)
						{ return retStr; }
						command.CommandType = CommandType.StoredProcedure;
						SqlParameter scheme = new SqlParameter("@scheme", SqlDbType.NVarChar, 64);
						scheme.Direction = ParameterDirection.Input;
						scheme.Value = nORage;
						command.Parameters.Add(scheme);
						SqlParameter serialNo = new SqlParameter("@serialNo", SqlDbType.NVarChar, -1);
						serialNo.Direction = ParameterDirection.Output;
						command.Parameters.Add(serialNo);
						SqlParameter errCode = new SqlParameter("@errCode", SqlDbType.Int, int.MaxValue);
						errCode.Direction = ParameterDirection.Output;
						command.Parameters.Add(errCode);
						SqlParameter errMsg = new SqlParameter("@errMsg", SqlDbType.NVarChar, -1);
						errMsg.Direction = ParameterDirection.Output;
						command.Parameters.Add(errMsg);
						command.ExecuteNonQuery();
						if ((int)errCode.Value != 0)
						{
							SWSNG.errCode = (int)errCode.Value;
							SWSNG.errMsg = @"SQL Error: " + (string)errMsg.Value;
							return retStr;
						}
						if ((string)serialNo.Value == string.Empty)
						{
							SWSNG.errCode = 200;
							SWSNG.errMsg = @"Error reading the next Serial No.";
							return retStr;
						}
						retStr = (string)serialNo.Value;
					}
				}
			}
			catch (Exception ex)
			{
				SWSNG.errCode = 666;
				SWSNG.errMsg = ex.Message;
			}
			return retStr;
		}
	}
}
