﻿using System.Security.Cryptography;

namespace services;

/* Данный Singleton-сервис служит для валидации мастер-аккаунтов.
 */
public sealed class MasterAccountsService
{
	private readonly byte[][] _mastersKeyHashes;

	public MasterAccountsService(SettingsProviderService settingsProvider)
	{
		_mastersKeyHashes = settingsProvider.MasterAccounts
			.Select(x => Convert.FromBase64String(x.KeyHashBase64)).ToArray();
	}

	/// <returns>true if passed key is valid, false otherwise.</returns>
	public bool IsValid(string keyBase64)
	{
		byte[] search = SHA512.HashData(Convert.FromBase64String(keyBase64));
		return _mastersKeyHashes.Any(k => k.SequenceEqual(search));
	}
}
