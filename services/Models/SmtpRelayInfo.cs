using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace services.Models;
public class SmtpRelayInfo
{
	public string Login { get; set; } = null!;
	public string Password { get; set; } = null!;
	public string SmtpHost { get; set; } = null!;
	public int SmtpPort { get; set; }
	public int MaxMailsPerDay { get; set; }
}
