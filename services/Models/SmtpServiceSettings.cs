using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace services.Models;
public class SmtpServiceSettings
{
	public SmtpRelayInfo[] SmtpRelays { get; set; } = null!;
}

