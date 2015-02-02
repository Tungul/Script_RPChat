//-----------------------------------------------------------------------------
// RPChat
// SolarFlare
//  Modified by Clockturn
// Jesus guys, that's like a dozen KB of shit. - Lugnut
//-----------------------------------------------------------------------------

exec("./ServerCommands.cs");
exec("./Admin.cs");
exec("./misc.cs");
exec("./datablocks.cs");

$RPChat::Warning = "This is a <font:Arial Bold:14><color:FF0000>Role Playing<color:000000><font:Arial:14> server.<br><br><font:Arial:14>Use the command <color:FF0000><font:Arial Italic:14>/setName <first> <last><font:Arial:14> <color:000000>to change your name.";
$RPChat::MsgRange = 10;
$RPChat::Enabled = true;

//-----------------------------------------------------------------------------
// Prefs and shit

function servercmdrpcsave(%client)
{
	if(%client.isAdmin)
	{
		export("$RPChat::*","config/server/RPChatSave.cs");
		messageAll('',"\c6  All RPChat data has been saved.");
	}
}
exec("config/server/RPChatSave.cs");

$pref::clickstatsenabled = 1;

if(!$RPChatTemp__Prefs)
{
	if($AddOn__System_ReturnToBlockland)
	{
		if(!$RTB::RTBR_ServerControl_Hook)
		{
			exec("Add-Ons/System_ReturnToBlockland/RTBR_ServerControl_Hook.cs");
		}
		RTB_registerPref("Enabled", "RP Chat", "$RPChat::Enabled", "bool", "Script_RPChat", $RPChat::Enabled, 0, 0);
		RTB_registerPref("Speech Radius", "RP Chat", "$RPChat::MsgRange", "int 5 25", "Script_RPChat", $RPChat::MsgRange, 0, 0);
		RTB_registerPref("/shutdown auto-password", "RP Chat", "$Pref::Server::SecretPassword", "string 25", "Script_RPChat", $Pref::Server::SecretPassword, 0, 0);
		RTB_registerPref("X has entered the...", "RP Chat", "$Pref::Server::BluzoneLandName", "string 25", "Script_RPChat", $Pref::Server::BluzoneLandName, 0, 0);
		RTB_registerPref("Click-Stats Enabled", "RP Chat", "$Pref::ClickStatsEnabled", "bool", "Script_RPChat", 1, 0, 0);
		RTB_registerPref("Require last names", "RP Chat", "$RPChat::RequireLastNames", "bool", "Script_RPChat", $RPChat::RequireLastNames, 0, 0);
		RTB_registerPref("Autoban systems", "RP Chat", "$RPChat::AutoBan", "bool", "Script_RPChat", $RPChat::AutoBan, 0, 0);
	}
	$RPChatTemp__Prefs = 1;
}

//-----------------------------------------------------------------------------
// RPChat

deactivatepackage("RPChat");
deactivatePackage("lugsNewServerPack");
deactivatepackage("evalchat");
package RPChat
{
	function serverCmdClearAllBricks(%c)
	{
		bluzoneLog(%c.getplayerName() SPC "cleared all bricks.");
		return parent::serverCmdClearAllBricks(%c);
	}
	function serverCmdReloadBricks(%c)
	{
		bluzoneLog(%c.getplayerName() SPC "reloaded all bricks.");
		return parent::serverCmdReloadBricks(%c);
	}
	function Player::mountImage(%this, %img, %slot)
	{
		if(%slot == 0 && %this.isHolding)
		{
			messageClient(%this.client, '', "Your hands are full! You can't hold an item right now!");
			return;
		}
		parent::mountImage(%this, %img, %slot);
	}
	function GameConnection::autoAdminCheck(%this)
	{
		%this.listeningtoooc = 1;
		%this.listeningtoradio = 1;
		if(%this.getRPName() !$= "" && $Pref::Server::BluzoneLandName !$= "")
			messageAll('', "\c4" @ %this.getRPName() SPC "has re-entered the" SPC $Pref::Server::BluzoneLandName @ ".");
		messageClient(%this, '', "You are now listening to the OOC channel by default, and your radio has been turned on.");
		return parent::autoAdminCheck(%this);
	}
	function Player::activateStuff(%this)
	{
		%v = Parent::activateStuff(%this);
		%client = %this.client;
		if(!$pref::clickstatsenabled)
			return %v;
		
		%target = containerRayCast(%this.getEyePoint(), vectorAdd(vectorScale(vectorNormalize(%this.getEyeVector()), 3), %this.getEyePoint()), $TypeMasks::PlayerObjectType, %this);
		
		if(!isObject(%target.client) || !isObject(%target) || %target == %this || %this.getObjectMount() == %target)
			return %v;
		
		// get target's stats
		%name = %target.client.getRPName(); // target's name
		%health = %target.getDatablock().maxDamage - %target.getDamageLevel(); // target's health
		%status = %target.client.getRPStatus();
		if(%status $= "")
			%status = "N/A";
		
		// health color effects
		if(%health > 100)
			%health = "\c5" @ %health;
		if(%health < 101)
			%health = "\c2" @ %health;
		if(%health < 71)
			%health = "\c3" @ %health;
		if(%health < 36)
			%health = "\c0" @ %health;
		
		// display stats
		commandToClient(%client, 'centerPrint', "<just:right><font:palatino linotype:22>\c6RP Name: \c3" @ %name @ "<br>\c6Health: \c3" @ %health @ "<br>\c6Status: \c3" @ %status, 5);
		return %v;
	}
	function gameConnection::onClientEnterGame(%this)
	{
		parent::onClientEnterGame(%this);
		schedule(5000, 0, commandToClient, %this, 'MessageBoxOK', "Notice", $RPChat::Warning);
	}
	function serverCmdMessageSent(%client, %text)
	{
		if(%text $= "")
			return;
		%text = strReplace(%text, "<", "");
		if(!%client.hasSpawnedOnce && $RPChat::Enabled)
		{
			if(getSubStr(%text, 0, 1) $= "^")
				%text = getSubStr(%text, 1, strLen(%text));
			serverCmdTeamMessagesent(%client, "^" @ %text);
			return;
		}
		if($RPChat::Enabled)
		{
			%message = stripMLControlChars(%text);
			if(%client.getRPName() !$= "")
			{
				if(getSubStr(%text, 0, 1) $= ">")
				{
					InitContainerRadiusSearch(%client.player.getTransform(), 4, $TypeMasks::PlayerObjectType);
					while ((%targetObject = containerSearchNext()) != 0) 
					{
						if(%targetObject.client !$= "")
						{
							%message = getSubStr(%text, 1, strLen(%message));
							if(!%targetObject.client.canHearAll)
								messageclient(%targetObject.client, '', "\c7[" @ getSubStr(%client.getPlayerName(), 0, 4) @ "]\c3" SPC %client.getRPName() @ " whispers,\c6 " @ %message);
							if(!%targetObject.isHiding)
								%targetObject.emote(localChatProjectile, 1);
							if(%targetObject.client !$= %client)
								%PEEPSWHOHEARD = %PEEPSWHOHEARD SPC %targetObject.client.name;
						}
					}
					for(%i = 0; %i < clientGroup.getCount(); %i++)
					{
						%cl = clientGroup.getObject(%i);
						if(!%cl.canhearall)
							continue;
						messageClient(%cl, '', "[HEARALL]\c3" SPC "\c2[" @ %client.getPlayerName() @ "]\c3" SPC %client.getRPName() SPC "whispers, \"" @ %message @ "\"" SPC "\c5HEARD BY\c6:" SPC %PEEPSWHOHEARD);
					}
					bluzoneLog(%client.getRPName() SPC "whispers, \"" @ %message @ "\"" TAB "HEARD BY:" SPC %PEEPSWHOHEARD, 1, 1);
					return;
				}
				InitContainerRadiusSearch(%client.player.getTransform(), $RPChat::MsgRange, $TypeMasks::PlayerObjectType);
				while ((%targetObject = containerSearchNext()) != 0) 
				{
					if(%targetObject.client !$= "")
					{
						if(getSubStr(%text, 0, 1) $= "^")
						{
							%message = getSubStr(%text, 1, strLen(%message));
							if(!%targetObject.client.canHearAll)
								messageClient(%targetObject.client, '', "\c7[" @ getSubStr(%client.getPlayerName(), 0, 4) @ "]\c3" SPC %client.getRPName() SPC %message);
							if(!%targetObjec.isHiding)
								%targetObject.emote(localChatProjectile, 1);
							if(%targetObject.client !$= %client)
								%PEEPSWHOHEARD = %PEEPSWHOHEARD SPC %targetObject.client.name;
								%ISEMOTE = 1;
						}
						else
						{
							if(!%targetObject.client.canHearAll)
								messageClient(%targetObject.client, '', "\c7[" @ getSubStr(%client.getPlayerName(), 0, 4) @ "]\c3" SPC %client.getRPName() SPC "says, \"\c6" @ %message @ "\c3\"");
							if(!%targetobject.ishiding)
								%targetObject.emote(localChatProjectile, 1);
							if(%targetObject.client !$= %client)
								%PEEPSWHOHEARD = %PEEPSWHOHEARD SPC %targetObject.client.name;
						}
					}
				}
				for(%i = 0; %i < clientGroup.getCount(); %i++)
				{
					%cl = clientGroup.getObject(%i);
					if(!%cl.canhearall)
						continue;
					if(getSubStr(%text, 0, 1) $= "^")
					{
						%message = getSubStr(%text, 1, strLen(%message));
						messageClient(%cl, '', "[SEEALL]\c3" SPC "\c2[" @ %client.getPlayerName() @ "]\c3" SPC %client.getRPName() SPC %message SPC "\c5SEEN BY\c6:" SPC %PEEPSWHOHEARD);
						continue;
					}
					messageClient(%cl, '', "[HEARALL]\c3" SPC "\c2[" @ %client.getPlayerName() @ "]\c3" SPC %client.getRPName() SPC "says, \"" @ %message @ "\"" SPC "\c5HEARD BY\c6:" SPC %PEEPSWHOHEARD);
				}
				if(%ISEMOTE)
				{
					bluzoneLog(%client.getRPName() SPC %message TAB %PEEPSWHOHEARD, 1, 1);
				}
				else
				{
					bluzoneLog(%client.getRPName() SPC "says, \"" @ %message @ "\"" TAB %PEEPSWHOHEARD, 1, 1);
				}
			}
			else
				messageClient(%client, '', 'You have not specified a name using /setName.');
		}
		else
		{
			parent::serverCmdMessageSent(%client, %text);
		}
	}
	function serverCmdTeamMessageSent(%client, %text)
	{
		%text = stripTrailingSpaces(%text);
		%text = strReplace(%text, "<", "");
		if(getSubStr(%text, 0, 1) $= ">")
		{
			serverCmdMessageSent(%client, %text);
			return;
		}
		if(%text $= "")
			return;
		if($RPChat::Enabled)
		{
			%message = stripMLControlChars(%text);
			if(%client.getRPName() !$= "" || !%client.hasSpawnedOnce)
			{
				if(getSubStr(%text,0,1) !$= "^" && !$RPChat::RadioDisabled && %client.listeningtoradio)
				{
					for(%i = 0; %i < ClientGroup.getCount(); %i++)
					{
						%cl = ClientGroup.getObject(%i);
						%frs = $RPChat::Clientdata::MonitorFreqs[%cl.bl_id];
						if(%cl.listeningtoradio)
							messageClient(%cl, '', "\c7[" @ getSubStr(%client.getPlayerName(), 0, 4) @ "]\c3" SPC %client.getRPName() @ " broadcasts,\c6 " @ %message);
							if(%cl !$= %client)
								%PEEPSWHOHEARD = %PEEPSWHOHEARD SPC "[" @ %cl.name @ "]" SPC %cl.getRPName();
					}
					bluzoneLog(%client.getRPName() SPC "broadcasts, \"" @ %message @ "\"", 0, 0);// TAB %PEEPSWHOHEARD);
				} 
				else 
				{
					if(%client.listeningtoooc & $RPChat::OOCEnabled)
					{
						%message = getSubStr(%text, 1, strLen(%message));
						for(%i = 0; %i < ClientGroup.getCount(); %i++)
						{
							%cl = ClientGroup.getObject(%i);
							if(%cl.listeningtoooc)
							{
								chatMessageClient(%cl, %client, 0, 0, '\c3%2 [OOC]\c6: %4', "", %client.getPlayerName(), "", %message);
								if(%cl !$= %client)
									%PEEPSWHOHEARD = %PEEPSWHOHEARD SPC "[" @ %cl.name @ "]" SPC %cl.getRPName();
							}
						}
						bluzoneLog(%client.getPlayerName() SPC "[OOC]: " @ %message, 0, 1);// TAB %PEEPSWHOHEARD);
					}
				}
			}
			else
				messageClient(%client, '', 'You have not specified a name using /setName.');
		}
		else
		{
			parent::serverCmdTeamMessageSent(%client, %text);
		}
	}
};
activatepackage("RPChat");
activatePackage("lugsNewServerPack");
activatepackage("evalchat");

function GameConnection::getRPName(%client)
{
	%first = $RPChat::Clientdata::Firstname[%client.bl_id];
	%last = $RPChat::Clientdata::Lastname[%client.bl_id];
	%nick = $RPChat::Clientdata::Nickname[%client.bl_id];
	%q = $RPChat::Clientdata::NoQuotes[%client.bl_id];
	if(%first $= "" || %last $= "")
	{
		return "";
	}
	return %first SPC ((%nick !$= "" && !%q) ? "\"" @ %nick @ "\" " : "") @ ((%nick !$= "" && %q) ? %nick @ " " : "") @ %last;
}

function GameConnection::getRPStatus(%this)
{
	return %this.rpStatus;
}

function findRPNameByBL_ID(%id)
{
	%first = $RPChat::Clientdata::Firstname[%id];
	%last = $RPChat::Clientdata::Lastname[%id];
	%nick = $RPChat::Clientdata::Nickname[%id];
	%q = $RPChat::Clientdata::NoQuotes[%id];
	if(%first $= "" || %last $= "")
	{
		return "Nobody";
	}
	return %first SPC ((%nick !$= "" && !%q) ? "\"" @ %nick @ "\" " : "") @ ((%nick !$= "" && %q) ? %nick @ " " : "") @ %last;
}

function BluzoneLog(%text)
{
	if(%text $= "")
		return;
	%date = strreplace(getsubstr(getDateTime(),0,8),"/","-");
	%time = getsubstr(getDateTime(),9,8);
	%file = new FileObject();
	%file.openForAppend("config/server/blulogs/" @ %date @ ".txt");
	%file.writeline("[" @ %time @ "]" SPC %text);
	%file.close();
	%file.delete();
}

function Player::GetNearbyPeople(%this)
{
	if(!isObject(%this))
		return "ERROR 404, source player doesn't exist";
	InitContainerRadiusSearch(%this.getTransform(), 4, $TypeMasks::PlayerObjectType);
	while ((%targetObject = containerSearchNext()) != 0) 
	{
		if(%targetObject.client !$= "" && %targetObject != %this)
		{
			%PEEPSWHOHEARD = %PEEPSWHOHEARD @ %targetObject.client.getPlayerName() SPC "[" @ %targetObject.client.getRPName() @ "]" SPC "";
		}
	}
	return %peepswhoheard;
}