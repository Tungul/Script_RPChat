//-----------------------------------------------------------------------------
// Miscellanious

function smp()
{
	setModPaths(getModPaths());
}

package lugsNewServerPack
{
	function fcbn(%t)
	{
		return findclientbyname(%t);
	}
	function fpbn(%t)
	{
		return findclientbyname(%t).player;
	}
	function fcbb(%t)
	{
		return findclientbybl_id(%t);
	}
	function disp(%a)
	{
		messageall('',%a);
	}
	function getdate()
	{
		return getsubstr(getDateTime(),0,8);
	}
	function gettime()
	{
		return getsubstr(getDateTime(),9,8);
	}
};
activatePackage("lugsNewServerPack");

function randstr(%len, %chr)
{
	if(!strLen(%chr))
	{
		%chr = "abcdefghijklmnopqrstuvwxyz1234567890";
	}
	%str = "";
	%cln = strLen(%chr);
	if(!strLen(%len) || %len < 1)
	{
		return %str;
	}
	for(%i = 0 ; %i < %len ; %i++)
	{
		%str = %str @ getSubStr(%chr, getRandom(0, %cln - 1), 1);
	}
	return %str;
}
