using System;
using System.Configuration;
using System.Reflection;
using System.Text;
using System.Timers;
using ACASLibraries;

namespace ACASLibraries.Scheduling
{
	#region SchedulerConfigurationSection
	/// <summary>
	/// Scheduler configuration section definition.  Important note with Target attribute, if the class/method is located in an assembly aside from the executing assembly
	/// the assembly name (without .dll extension) is required for reference.  Otherwise this is not required.  trailing parentheses () are not required for referencing the
	/// target method.  Target methods should be defined as static (c#) or shared (vb).
	/// </summary>
	/// <code>
	/// &lt;configuration&gt;
	/// 	&lt;configSections&gt;
	/// 				&lt;section name="Scheduler" type="ACASLibraries.Scheduling.SchedulerConfigurationSection, ACASLibraries.Scheduling"/&gt;
	/// 	&lt;/configSections&gt;
	/// 
	///		&lt;!--...snip app.config or web.config code...--&gt;
	/// 
	///		&lt;Scheduler&gt;
	///			&lt;Tasks&gt;
	///				&lt;ScheduledTask Name="test1" Target="Namespace.Class.StaticMethod(), AssemblyName" Frequency="Hourly" StartTime="14:35"/&gt;
	///				&lt;ScheduledTask Name="test2" Target="Namespace.Class.StaticMethod, AssemblyName" Frequency="Minutes" MinutesInterval="5"/&gt;
	///				&lt;ScheduledTask Name="test3" Target="Namespace.Class.StaticMethod()" ExecuteOnStartup="true" Frequency="Daily" StartTime="14:45"/&gt;
	///				&lt;ScheduledTask Name="test4" Target="Namespace.Class.StaticMethod()" Frequency="Weekly" StartTime="08:45" DayOfWeek="Tuesday"/&gt;
	///				&lt;ScheduledTask Name="test5" Target="Namespace.Class.StaticMethod()" Frequency="Monthly" StartTime="09:15" DayOfMonth="14"/&gt;
	///			&lt;/Tasks&gt;
	///		&lt;/Scheduler&lt;
	/// &lt;configuration&gt;
	/// </code>
	public class SchedulerConfigurationSection : ConfigurationSection
	{
		[ConfigurationProperty("Tasks",IsRequired=true,IsDefaultCollection=true)]
		[ConfigurationCollection(typeof(ScheduledTaskConfigurationElement),AddItemName="ScheduledTask")]
		public ScheduledTaskConfigurationElementCollection ScheduledTasks
		{
			get
			{
				return (ScheduledTaskConfigurationElementCollection)this["Tasks"];
			}
		}
	}
	#endregion

	#region ScheduledTaskConfigurationElementCollection
	public class ScheduledTaskConfigurationElementCollection : ConfigurationElementCollection
	{
		public ScheduledTaskConfigurationElementCollection()
		{
			ScheduledTaskConfigurationElement oElement = (ScheduledTaskConfigurationElement)CreateNewElement();
		}
		protected override ConfigurationElement CreateNewElement()
		{
			return new ScheduledTaskConfigurationElement();
		}
		protected override ConfigurationElement CreateNewElement(string ElementName)
		{
			return new ScheduledTaskConfigurationElement(ElementName);
		}
		protected override Object GetElementKey(ConfigurationElement Element)
		{
			return ((ScheduledTaskConfigurationElement)Element).Name;
		}

		public new ScheduledTaskConfigurationElement this[string Name]
		{
			get
			{
				return (ScheduledTaskConfigurationElement)this.BaseGet(Name);
			}
		}
	
		public ScheduledTaskConfigurationElement this[int Index]
		{
			get
			{
				return (ScheduledTaskConfigurationElement)this.BaseGet(Index);
			}
		}
	}
	#endregion

	#region ScheduledTaskConfigurationElement
	public class ScheduledTaskConfigurationElement : ConfigurationElement
	{
		public ScheduledTaskConfigurationElement()
		{ }
		public ScheduledTaskConfigurationElement(string ElementName)
		{
			this.Name = ElementName;
		}
		
		[ConfigurationProperty("Name", IsRequired=true, IsKey=true)]
		public string Name
		{
			get { return (string)this["Name"]; }
			set { this["Name"] = value; }
		}
		[ConfigurationProperty("TaskType", DefaultValue = ScheduleTaskType.StaticMethod, IsRequired = false)]
		public ScheduleTaskType TaskType
		{
			//get { return (string)this["TaskType"]; }
			//set { this["TaskType"] = value; }
			get
			{
				switch(this["TaskType"].ToString().ToLower())
				{
					case "1":
					case "staticmethod":
						return ScheduleTaskType.StaticMethod;
					case "2":
					case "executable":
						return ScheduleTaskType.Executable;
					default:
						return ScheduleTaskType.StaticMethod;
				}
			}
			set
			{
				switch(value)
				{
					case ScheduleTaskType.StaticMethod:
						this["TaskType"] = "StaticMethod";
						break;
					case ScheduleTaskType.Executable:
						this["TaskType"] = "Executable";
						break;
					default:
						this["TaskType"] = "StaticMethod";
						break;
				}
			}
		}
		[ConfigurationProperty("Target", IsRequired = true)]
		public string Target
		{
			get { return (string)this["Target"]; }
			set { this["Target"] = value; }
		}
		[ConfigurationProperty("TargetParameters", DefaultValue = "", IsRequired = false)]
		public string TargetParameters
		{
			get { return (string)this["TargetParameters"]; }
			set { this["TargetParameters"] = value; }
		}
		[ConfigurationProperty("Frequency", DefaultValue = "Daily", IsRequired = false)]
		public ScheduleFrequency Frequency
		{
			get
			{
				switch(this["Frequency"].ToString().ToLower())
				{
					case "1":
					case "hourly":
						return ScheduleFrequency.Hourly;
					case "2":
					case "weekly":
						return ScheduleFrequency.Weekly;
					case "4":
					case "monthly":
						return ScheduleFrequency.Monthly;
					case "16":
					case "minutes":
						return ScheduleFrequency.Minutes;
					default:
						return ScheduleFrequency.Daily;
				}
			}
			set
			{
				switch(value)
				{
					case ScheduleFrequency.Hourly:
						this["Frequency"] = "Hourly";
						break;
					case ScheduleFrequency.Weekly:
						this["Frequency"] = "Weekly";
						break;
					case ScheduleFrequency.Monthly:
						this["Frequency"] = "Monthly";
						break;
					case ScheduleFrequency.Minutes:
						this["Frequency"] = "Minutes";
						break;
					default:
						this["Frequency"] = "Daily";
						break;
				}
			}
		}
		[ConfigurationProperty("DayOfMonth", IsRequired = false, DefaultValue=0)]
		public int DayOfMonth
		{
			get { return (int)this["DayOfMonth"]; }
			set { this["DayOfMonth"] = value; }
		}
		[ConfigurationProperty("DayOfWeek", IsRequired = false)]
		public System.DayOfWeek DayOfWeek
		{
			get
			{
				switch (this["DayOfWeek"].ToString().ToLower())
				{
					case "1":
					case "monday":
						return DayOfWeek.Monday;
					case "2":
					case "tuesday":
						return DayOfWeek.Tuesday;
					case "3":
					case "wednesday":
						return DayOfWeek.Wednesday;
					case "4":
					case "thursday":
						return DayOfWeek.Thursday;
					case "5":
					case "friday":
						return DayOfWeek.Friday;
					case "6":
					case "saturday":
						return DayOfWeek.Saturday;
					default:
						return DayOfWeek.Sunday;
				}
			}
			set
			{
				switch (value)
				{
					case DayOfWeek.Monday:
						this["DayOfWeek"] = "Monday";
						break;
					case DayOfWeek.Tuesday:
						this["DayOfWeek"] = "Tuesday";
						break;
					case DayOfWeek.Wednesday:
						this["DayOfWeek"] = "Wednesday";
						break;
					case DayOfWeek.Thursday:
						this["DayOfWeek"] = "Thursday";
						break;
					case DayOfWeek.Friday:
						this["DayOfWeek"] = "Friday";
						break;
					case DayOfWeek.Saturday:
						this["DayOfWeek"] = "Saturday";
						break;
					default:
						this["DayOfWeek"] = "Sunday";
						break;
				}
			}
		}
		[ConfigurationProperty("StartDate", IsRequired = false)]
		public string StartDate
		{
			get { return (string)this["StartDate"]; }
			set { this["StartDate"] = value; }
		}
		[ConfigurationProperty("StartTime", DefaultValue = "00:00", IsRequired = false)]
		public string StartTime
		{
			get { return (string)this["StartTime"]; }
			set	{ this["StartTime"] = value; }
		}
		[ConfigurationProperty("MinutesInterval", DefaultValue=0.0, IsRequired = false)]
		public double MinutesInterval
		{
			get { return (double)this["MinutesInterval"]; }
			set { this["MinutesInterval"] = value; }
		}
        [ConfigurationProperty("ExecuteOnStartup", DefaultValue = false, IsRequired = false)]
        public bool ExecuteOnStartup
        {
            get { return (bool)this["ExecuteOnStartup"]; }
            set { this["ExecuteOnStartup"] = value; }
        }
	}
	#endregion

	#region ScheduleFrequency
	public enum ScheduleFrequency
	{
		Hourly=1,
		Daily=2,
		Weekly=4,
		Monthly=8,
		Minutes=16
	}
	#endregion

	#region ScheduleTaskType
	public enum ScheduleTaskType
	{
		StaticMethod = 1,
		Executable = 2
	}
	#endregion

	#region ScheduledTask
	public class ScheduledTask
	{
		public string Name = null;
		public ScheduleTaskType TaskType = ScheduleTaskType.StaticMethod;
		public string Target = null;
		public string TargetParameters = null;
		public ScheduleFrequency Frequency = ScheduleFrequency.Daily;
		public int DayOfMonth = 0;
		public System.DayOfWeek DayOfWeek = DayOfWeek.Sunday;
		public string StartDate = null;
		public string StartTime = null;
		public double MinutesInterval = 0;
        public bool ExecuteOnStartup = false;

		public DateTime NextRun
		{
			get
			{
				return dtNextRunRestricted;
			}
		}

		private DateTime dtNextRun = DateTime.Today.AddDays(-1);
		private DateTime dtNextRunRestricted = DateTime.Today.AddDays(-1);
		private Timer m_oTimer = new Timer();
		private bool m_bHasRunOnce = false;

		#region ScheduleTask();
		public void ScheduleTask()
		{
			try
			{
				Trace.Write("ScheduledTask.ScheduleTask()", Name + " (TaskType: " + TaskType.ToString() + ", Target: " + Target.ToString() + ", Parameters: " + TargetParameters.ToString() + ") scheduling...");
				m_oTimer.Stop();
                if (ExecuteOnStartup == true && m_bHasRunOnce == false)
                {
                    Execute(true);
                }
                DateTime dtNowClean = DateTime.Parse(DateTime.Now.ToString("M/d/yyyy HH:mm:00"));
                dtNextRunRestricted = GetNextRunDateTime();
                TimeSpan oTS1 = dtNextRunRestricted.Subtract(dtNowClean);
				if (oTS1.TotalMilliseconds > Int32.MaxValue)
				{
					dtNextRun = DateTime.Now.AddMilliseconds(Int32.MaxValue - 1);
				}
				else
				{
					dtNextRun = dtNextRunRestricted;
				}
                TimeSpan oTS = dtNextRun.Subtract(dtNowClean);
                m_oTimer.Interval = oTS.TotalMilliseconds;
                m_oTimer.Start();
				Trace.Write("ScheduledTask.ScheduleTask()", Name + " (TaskType:" + TaskType.ToString() + ", Target: " + Target.ToString() + ", Parameters: " + TargetParameters.ToString() + ") scheduled to run " + NextRun.ToString("s"));
			}
			catch (Exception oException)
			{
				Logger.LogError("ScheduledTask.ScheduleTask()", Name + " (TaskType:" + TaskType.ToString() + ", Target: " + Target.ToString() + ", Parameters: " + TargetParameters.ToString() + ")", oException);
				Trace.Write(oException);
			}
		}
		#endregion

		#region StopTask();
		public void StopTask()
		{
			m_oTimer.Stop();
		}
		#endregion

		#region GetNextRunDateTime();
		public DateTime GetNextRunDateTime()
		{
			DateTime dtStart = NextRun;
			if(!m_bHasRunOnce)
			{
				string sStartDate = null;
				string sStartTime = null;

				DateTime dtNow = DateTime.Now;

				//set date from task settings
				if (StartDate != null)
				{
					sStartDate = StartDate;
				}
				else
				{
					sStartDate = DateTime.Today.ToString("M/d/yyyy");
				}
				if (StartTime != null)
				{
					sStartTime = StartTime;
				}
				else
				{
					sStartTime = DateTime.Today.ToString("HH:mm:00");
				}

				dtStart = DateTime.Parse(sStartDate+" "+sStartTime);
				if (dtStart < DateTime.Today)
				{
					//move date to today
					dtStart = DateTime.Parse(DateTime.Today.ToString("M/d/yyyy") + " " + sStartTime);
				}

				//move date/time to correct first run time
				switch (Frequency)
				{
					case ScheduleFrequency.Minutes:
						if (dtStart <= dtNow && MinutesInterval > 0)
						{
							while (dtStart < dtNow)
							{
								dtStart = dtStart.AddMinutes(MinutesInterval);
							}
						}
						break;
					case ScheduleFrequency.Hourly:
						if (dtStart < dtNow)
						{
							while (dtStart < dtNow)
							{
								dtStart = dtStart.AddHours(1);
							}
						}
						break;
					case ScheduleFrequency.Daily:
						if (dtStart <= dtNow)
						{
							dtStart = dtStart.AddDays(1);
						}
						break;
					case ScheduleFrequency.Weekly:
						if (dtStart.DayOfWeek != this.DayOfWeek || dtStart <= dtNow)
						{
							int iCurrentWeekday = (int)dtStart.DayOfWeek;
							int iTargetWeekday = (int)this.DayOfWeek;
							if (iCurrentWeekday > iTargetWeekday)
							{
								dtStart = dtStart.AddDays((double)(6 - iCurrentWeekday + iTargetWeekday + 1));
							}
							else if (iCurrentWeekday < iTargetWeekday)
							{
								dtStart = dtStart.AddDays((double)(iTargetWeekday - iCurrentWeekday));
							}
							else
							{
								dtStart = dtStart.AddDays(7);
							}
						}
						break;
					case ScheduleFrequency.Monthly:
						if (dtStart <= dtNow)
						{
							dtStart = dtStart.AddMonths(1);
						}
						break;
				}
			}
			else
			{
				switch (Frequency)
				{
					case ScheduleFrequency.Minutes:
						dtStart = NextRun.AddMinutes(MinutesInterval);
						break;
					case ScheduleFrequency.Hourly:
						dtStart = NextRun.AddHours(1);
						break;
					case ScheduleFrequency.Daily:
						dtStart = NextRun.AddDays(1);
						break;
					case ScheduleFrequency.Weekly:
						dtStart = NextRun.AddDays(7);
						break;
					case ScheduleFrequency.Monthly:
						dtStart = NextRun.AddMonths(1);
						break;
				}
			}
			return dtStart;
		}
		#endregion

		#region Execute();
        public void Execute()
        {
            Execute(false);
        }

		private static void AppendAddresses(string Addresses, ref System.Net.Mail.MailMessage Message, string AddressType)
		{
			if (Addresses != null)
			{
				if (Addresses.IndexOf(";") > 0)
				{
					string[] saAddresses = Addresses.Split(";".ToCharArray());
					for (int x = 0; x < saAddresses.Length; x++)
					{
						if (AddressType.ToUpper() == "TO")
						{
							Message.To.Add(new System.Net.Mail.MailAddress(saAddresses[x]));
						}
						else
						{
							Message.CC.Add(new System.Net.Mail.MailAddress(saAddresses[x]));
						}
					}
				}
				else
				{
					if (AddressType.ToUpper() == "TO")
					{
						Message.To.Add(new System.Net.Mail.MailAddress(Addresses));
					}
					else
					{
						Message.CC.Add(new System.Net.Mail.MailAddress(Addresses));
					}
				}
			}
		}

		private void Execute(bool StartupExecution)
		{
			Trace.Write("ScheduledTask.Execute()", Name + " (TaskType: " + TaskType.ToString() + ", Target: " + (Target != null ? Target : "NULL") + ", Parameters: " + (TargetParameters.Length > 0 ? TargetParameters : "[none]") + ") execute start");
			if (Target != null && Target.Length > 0)
			{
				if (TaskType == ScheduleTaskType.Executable)
				{
					bool bExecutionSuccess = false;
					string sResults = "";
					try
					{
						string sTargetFilenameWithPath = null;
						string sTargetArguments = "";
						sTargetFilenameWithPath = Target.Trim();
						sTargetArguments = TargetParameters.Trim();
						Trace.Write("ScheduledTask.Execute()", "Filename (with path): " + sTargetFilenameWithPath + ", Parameters: " + sTargetArguments);
						try
						{
							Logger.LogCustomMessage("Running Scheduled Process: " + sTargetFilenameWithPath + " (parameters: " + ((sTargetArguments.Length > 0) ? sTargetArguments : "[none]") + ")");
						}
						catch (Exception oException)
						{
						}
						//System.Diagnostics.Process.Start(@sTargetFilenameWithPath, @sTargetArguments);
						int iMaximumRunDerationInSeconds = 1200000; // 20 minutes
						if (Parser.ToInt(ConfigurationManager.AppSettings["MaximumRunDerationInSeconds"]) > 0)
						{
							iMaximumRunDerationInSeconds = Parser.ToInt(ConfigurationManager.AppSettings["MaximumRunDerationInSeconds"]) * 1000;
						}
						System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(@sTargetFilenameWithPath, @sTargetArguments);
						psi.RedirectStandardOutput = true;
						psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
						psi.UseShellExecute = false;
						System.Diagnostics.Process listFiles;
						listFiles = System.Diagnostics.Process.Start(psi);
						System.IO.StreamReader myOutput = listFiles.StandardOutput;
						listFiles.WaitForExit(iMaximumRunDerationInSeconds);
						if (listFiles.HasExited)
						{
							string output = myOutput.ReadToEnd();
							sResults = output.Trim();
						}
						Logger.LogCustomMessage("Task Complete - output from process:\r\n" + (sResults.Length > 0 ? sResults : "[none]"));
						bExecutionSuccess = true;
					}
					catch (Exception oException)
					{
						Logger.LogError("ScheduledTask.Execute()", oException);
						bExecutionSuccess = false;
					}
					string sAddressFrom = "";
					string sAddressTo = "";
					string sSubject = "";
					string sBody = "";
					try
					{
						string sEmailScheduledProcessStatus = Parser.ToString(ConfigurationManager.AppSettings["EmailScheduledProcessStatus"]);
						if (sEmailScheduledProcessStatus.Length > 0 && Parser.ToBool(sEmailScheduledProcessStatus) == true)
						{
							sAddressFrom = Parser.ToString(ConfigurationManager.AppSettings["AddressFrom"]);
							sAddressTo = Parser.ToString(ConfigurationManager.AppSettings["AddressTo"]);
							sSubject = Parser.ToString(ConfigurationManager.AppSettings["MessageSubject"]);
							string sProcInfo = "Task: " + Name + " (" + TaskType.ToString() + ")\r\nMachine: " + System.Environment.MachineName.ToString() + "\r\nTarget: " + (Target != null ? Target : "NULL") + "\r\nParameters: " + (TargetParameters.Length > 0 ? TargetParameters : "[none]") + "\r\n";
							if (bExecutionSuccess)
							{
								sBody = "Process Launched\r\n\r\n" + sProcInfo + "Output from process:\r\n" + (sResults.Length > 0 ? sResults : "[none]") + "\r\n";
							}
							else
							{
								sBody = "Proccess FAILED TO LAUNCH!\r\n\r\n" + sProcInfo + "Please consult logfile for details.\r\n";
							}
							System.Net.Mail.MailMessage oMessage = new System.Net.Mail.MailMessage();
							oMessage.From = new System.Net.Mail.MailAddress(sAddressFrom);
							AppendAddresses(sAddressTo, ref oMessage, "To");
							oMessage.Subject = sSubject;
							oMessage.Body = sBody;
							System.Net.Mail.SmtpClient oSmtpClient = new System.Net.Mail.SmtpClient();
							oSmtpClient.Send(oMessage);
							oSmtpClient = null;
							oMessage = null;
						}
					}
					catch (Exception oException)
					{
						Logger.LogError("ERROR sending mail", "From: " + sAddressFrom + "\r\nTo: " + sAddressTo + "\r\nSubject: " + sSubject + "\r\nBody: " + sBody, oException);
					}
				}
				else
				{
					try
					{
						string sTargetAssembly = null;
						string sTargetClass = null;
						string sTargetMethod = null;
						if (Target.IndexOf(",") > 0)
						{
							sTargetAssembly = Target.Substring(Target.LastIndexOf(",") + 1, Target.Length - Target.LastIndexOf(",") - 1);
							sTargetClass = Target.Substring(0, Target.LastIndexOf(".")).Trim();
							sTargetMethod = Target.Substring(Target.LastIndexOf(".") + 1, Target.LastIndexOf(",") - Target.LastIndexOf(".") - 1).Replace("()", "").Replace(";", "").Trim();
							Trace.Write("ScheduledTask.Execute()", "Assembly: " + sTargetAssembly + ", Class: " + sTargetClass + ", Method: " + sTargetMethod);
						}
						else
						{

							Trace.Write("ScheduledTask.Execute()", "Class: " + sTargetClass + ", Method: " + sTargetMethod);
						}
						Type tClass = Type.GetType(sTargetClass + (sTargetAssembly != null && sTargetAssembly.Length > 0 ? ", " + sTargetAssembly : ""));
						if (tClass != null)
						{
							object oResult = tClass.InvokeMember(sTargetMethod, BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, null);
							Trace.Write("ScheduledTask.Execute()", "Method output: " + (oResult != null ? oResult.ToString() : "NULL"));
						}
						else
						{
							Trace.Write("ScheduledTask.Execute()", "Could not locate type " + sTargetClass);
							Logger.LogError("ScheduledTask.Execute()", "Could not locate type " + sTargetClass);
						}
					}
					catch (Exception oException)
					{
						Logger.LogError("ScheduledTask.Execute()", oException);
					}
				}
			}
            if (!StartupExecution)
            {
                m_bHasRunOnce = true;
            }
			Trace.Write("ScheduledTask.Execute()", Name + " (TaskType: " + TaskType.ToString() + ", Target: " + (Target != null ? Target : "NULL") + ", Parameters: " + TargetParameters.ToString() + ") execute end");
		}
		#endregion

		#region m_oTimer_Elapsed();
		void m_oTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			m_oTimer.Stop();
			if (DateTime.Now >= dtNextRunRestricted)
			{
				Execute();
			}
            else if (((TimeSpan)dtNextRunRestricted.Subtract(DateTime.Now)).TotalSeconds <= 5 && MinutesInterval == 0)
            {
                //wait a second and then run if within 5 seconds for non-minute schedules
                System.Threading.Thread.Sleep(int.Parse(((TimeSpan)dtNextRunRestricted.Subtract(DateTime.Now)).TotalMilliseconds.ToString()));
                Execute();
            }
            else
            {
                Trace.Write("ScheduledTask.m_oTimer_Elapsed()", "Skipping execution due to run restriction of " + dtNextRunRestricted.ToString("s") + " (this may happen between long run intervals)");
            }
			ScheduleTask();
		}
		#endregion

		#region Constructor
		public ScheduledTask()
		{ }
		public ScheduledTask(ScheduledTaskConfigurationElement Element)
		{
			this.Name = Element.Name;
			this.TaskType = Element.TaskType;
			this.Target = Element.Target;
			this.TargetParameters = Element.TargetParameters;
			this.Frequency = Element.Frequency;
			this.DayOfMonth = Element.DayOfMonth;
			this.DayOfWeek = Element.DayOfWeek;
			this.StartDate = Element.StartDate;
			this.StartTime = Element.StartTime;
			this.MinutesInterval = Element.MinutesInterval;
            this.ExecuteOnStartup = Element.ExecuteOnStartup;
			m_oTimer.Elapsed += new ElapsedEventHandler(m_oTimer_Elapsed);
		}
		#endregion
	}
	#endregion

	#region Scheduler (static)
	public static class Scheduler
	{
		public static System.Collections.Generic.List<ScheduledTask> ScheduledTasks = null;
		public static void LoadScheduledTasks()
		{
			LoadScheduledTasks(true);
		}
		public static void LoadScheduledTasks(bool AutoScheduleTasks)
		{
			SchedulerConfigurationSection oSection = (SchedulerConfigurationSection)ConfigurationManager.GetSection("Scheduler");
			ScheduledTasks = new System.Collections.Generic.List<ScheduledTask>(oSection.ScheduledTasks.Count);
			foreach (ScheduledTaskConfigurationElement oTaskConfig in oSection.ScheduledTasks)
			{
				ScheduledTasks.Add(new ScheduledTask(oTaskConfig));
				Trace.Write("Scheduler.LoadScheduledTasks()", "Added scheduled task " + oTaskConfig.Name + " (TaskType: " + oTaskConfig.TaskType.ToString() + ", Target: " + oTaskConfig.Target.ToString() + ", Parameters: " + oTaskConfig.TargetParameters.ToString() + ")");
				if (AutoScheduleTasks)
				{
					ScheduledTasks[ScheduledTasks.Count - 1].ScheduleTask();
				}
			}
			oSection = null;
		}
		public static void ScheduleAllTasks()
		{
			if(ScheduledTasks != null)
			{
				for(int x=0;x<ScheduledTasks.Count;x++)
				{
					ScheduledTasks[x].ScheduleTask();
				}
			}
		}
	}
	#endregion
}