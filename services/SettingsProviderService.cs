using Microsoft.Extensions.Configuration;
using services.Models;
using vdb_node_api.Models.Runtime;

namespace services;

/* Sigleton-сервис, служит для повышения уровня абстракции в других сервисах.
 * Обеспечивает получение настроек из appsettings и прочих файлов
 * с последующей их записью в соответствующие модели.
 */
public class SettingsProviderService
{
	protected readonly IConfiguration _configuration;
	public SettingsProviderService(IConfiguration configuration)
	{
		this._configuration = configuration;
	}


	public SmtpServiceSettings SmtpServiceSettings =>
		this._configuration.GetSection(nameof(this.SmtpServiceSettings))
		.Get<SmtpServiceSettings>() ?? new();

	public virtual MasterAccount[] MasterAccounts =>
		this._configuration.GetSection(nameof(this.MasterAccounts))
		.Get<MasterAccount[]>() ?? Array.Empty<MasterAccount>();
}
