//-----------------------------------------------------------------------------
// Server Commands (Admin)

function serverCmdRemoveSaveOwnership(%client)
{
	if(!%client.isSuperAdmin && %client !$= "webclient")
		return;
	new fileObject("Input");
	new fileObject("Output");
	Input.openForRead("base/server/temp/temp.bls");
	Output.openForWrite("base/server/temp/output.bls");
	while(!Input.isEOF())
	{
		%line = Input.readLine();
		if(strPos(%line, "+-OWNER") > -1)
			continue;
		Output.writeLine(%line);
	}
	Input.close();Input.delete();
	Output.close();Output.delete();
}

function serverCmdHearAll(%c)
{
	if(!%c.isadmin)
		return;
	%c.canhearall = !%c.canhearall;
	messageclient(%c, '', "\c3You can" SPC (%c.canhearall ? "now" : "no longer") SPC "hear everything.");
	bluzoneLog(%c.name SPC "can" SPC (%c.canhearall ? "now" : "no longer") SPC "hear everything.", 1);
}

function serverCmdSilentSetName(%client, %first, %last, %nick, %noquotes)
{
	if(!%client.isSuperAdmin)
		return;
	if(%first $= "" || %last $= "")
	{
		messageClient(%client, '', "One or more fields were not specified. Correct syntax: /setName <first> <last>");
		messageClient(%client, '', "Optionally, you can also include a nickname. Syntax: /setName <first> <last> <nickname>");
		return;
	}

	bluzoneLog("[SETNAME][SILENT]" SPC %client.getplayername() SPC "is now" SPC %client.getRPName(), 1);
	
	if(!%noquotes)
	{
		$RPChat::Clientdata::Firstname[%client.bl_id] = %first;
		$RPChat::Clientdata::Lastname[%client.bl_id] = %last;
		$RPChat::Clientdata::Nickname[%client.bl_id] = %nick;
	}
	else
	{
		$RPChat::Clientdata::Firstname[%client.bl_id] = %first;
		$RPChat::Clientdata::Nickname[%client.bl_id] = %last;
		$RPChat::Clientdata::Lastname[%client.bl_id] = %nick;
		$RPChat::Clientdata::NoQuotes[%client.bl_id] = 1;
	}

	for(%i = 0; %i < clientGroup.getCount(); %i++)
	{
		if(clientGroup.getObject(%i).isAdmin)
		{
			messageClient(clientGroup.getObject(%i), '', "[SILENT] \c6" @ %client.name @ "\c2 is now \c6" @ %client.getRPName() @ "\c2.");
		}
	}
}

function serverCmdAdminToggleOOC(%c)
{
	if($RPChat::OOCEnabled $= "")
		$RPChat::OOCEnabled = 1;
	if(!%c.issuperadmin)
		return;
	$RPChat::OOCEnabled = !$RPChat::OOCEnabled;
	messageAll('', %c.getPlayerName() SPC "has just" SPC ($RPChat::OOCEnabled ? "enabled" : "disabled") SPC "global OOC.");
}

function serverCmdGoCloak(%c)
{
	if(%c.isSuperAdmin && isObject(%c.player))
	{
		%c.player.hideNode("ALL");
		%c.player.setShapeNameDistance(0);
		%c.player.isHiding = 1;
	}
}

function serverCmdShutdown(%c)
{
	if(!%c.isAdmin)
		return %c.getPlayerName() SPC "is a fudgenugget";
	messageAll('', "\c3Server closing in 15 seconds.");
	schedule(15000, 0, bluzoneShutdown);
}

function bluzoneShutdown()
{
	$Pref::Server::Password = $Pref::Server::SecretPassword;
	shutdown();
}

function serverCmdPrivate(%c)
{
	if(!%c.isAdmin)
		return %c.getPlayerName() SPC "is a fudgenugget";
	messageAll('', "\c4Initializing private server settings macro...");
	%pass = randStr(5);
	messageAll('', "\c5Server password is \c3" @ %pass @ "\c5.");
	$Pref::Server::Password = %pass;
}

function serverCmdPublic(%c)
{
	if(!%c.isAdmin)
		return %c.getPlayerName() SPC "is a fudgenugget";
	$Pref::Server::Password = "";
}

function serverCmdRestoreBricks(%c)
{
	if(!%c.isSuperAdmin)
		return;
	fileCopy("base/server/temp/temp.bls", "base/server/temp/asdf.bls");
	fileCopy("base/server/temp/backup.bls", "base/server/temp/temp.bls");
}