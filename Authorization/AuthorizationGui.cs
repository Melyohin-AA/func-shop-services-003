using System;
using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Authorization;

internal static class AuthorizationGui
{
	public static IActionResult Get()
	{
		//~ add clear sakey cookie button
		return new ContentResult() {
			Content = 
@"<!DOCTYPE html>
<html lang='en'>

<head>
	<meta charset='UTF-8'>
	<title>Authorization - ShopServices</title>
</head>

<body>
	<label>Авторизация</label>
	<br/>
	<button id='applyButton' onclick='onApplyButtonClick()'>Применить</button>
	<input id='sakeyInput' placeholder='Код доступа' autofocus onkeydown='if (event.keyCode == 13) onApplyButtonClick()'/>
</body>

<script>
	var SAKEY_HEADER = '" + SimpleAccess.KeyHeader + @"';

	function onApplyButtonClick() {
		if (applyButton.disabled) return;
		sakey = sakeyInput.value.trim();
		sakeyInput.value = null;
		if (sakey.length == 0) {
			alert('Поле `Код доступа` является обязательным');
			return;
		}
		applySakey(sakey);
	}

	function applySakey(sakey) {
		const d = new Date();
		d.setTime(d.getTime() + 30*24*60*60*1000);
		let expires = 'expires=' + d.toUTCString();
		document.cookie = `${SAKEY_HEADER}=${sakey}; expires=${expires}; path=/`;
	}
</script>

</html>", ContentType = "text/html; charset=utf-8"};
	}
}
