//-----------------------------------------------------------------------------
// Server Commands

function serverCmdSetName(%client, %first, %last, %nick)
{
	if(%first $= "" || (%last $= "" && $RPChat::RequireLastNames))
	{
		messageClient(%client, '', "One or more fields were not specified. Correct syntax: /setName <first> <last>");
		messageClient(%client, '', "To add a nickname, add it onto the end. Syntax: /setName <first> <last> <nickname>");
		return;
	}
	
	%first = stripMLControlChars(%first);
	%last = stripMLControlChars(%last);
	%nick = stripMlControlChars(%nick);
	
	if(strLen(%first) > 16 || strLen(%last) > 16 || strLen(%nick) > 16)
	{
		messageClient(%client, '', "One of the specified names exceeded 16 characters.");
		return;
	}

	$RPChat::Clientdata::Firstname[%client.bl_id] = %first;
	$RPChat::Clientdata::Lastname[%client.bl_id] = %last;
	$RPChat::Clientdata::Nickname[%client.bl_id] = %nick;
	
	messageAll('', '\c6%1\c2 is now \c6%2\c2.', %client.getPlayerName(), %client.getRPName());
	bluzoneLog("[SETNAME]" SPC %client.getplayername() SPC "is now" SPC %client.getRPName(), 1);

	RPChatNameStupidParse(%client.bl_id); //This will remove < and > and capitalize the first letter of the name.
	
	//Include an all caps check?

	%rn = %client.getPlayerName();
	if(%rn $= %first || %rn $= %last) //Checks for their actual name in the rp name.
	{
		if(%client.rnwarned && $RPChat::AutoBan)
		{
			banBlid(%client.bl_id, 15, "AUTOBAN- Used real name in RP name.");
		}
		messageClient(%client, '', "\c3It is inadvisable to use your real name in your RP name. Get a new name.");

	}
	
	if((strInStr(%first) || strInStr(%last)) && $RPChat::AutoBan ) //Checks for swear words
	{
		// talk("You would be banned right now!");
		banBlid(%client.bl_id, 15, "AUTOBAN- ABUSE OF THE /SETNAME FUNCTION (CURSING) (you have no reason for that, srsly)");
	}
	if((%nick !$= "" && strInStr(%nick)) && $RPChat::AutoBan) //Checks for swear words
	{
		banBlid(%client.bl_id, 15, "AUTOBAN- ABUSE OF THE /SETNAME FUNCTION (CURSING) (you have no reason for that, srsly)");
	}
	
	if((getSubStr(%first, 0, 1) $= "<" || getSubStr(%last, 0, 1) $= "<" || getSubStr(%nick, 0, 1) $= "<") && $RPChat::AutoBan)
	{ //goddamned idiots
		//Checks for < and >. Note how we removed them above FROM THE NAME ITSELF, not the name in /setname that we set THE NAME ITSELF to.
		//It makes sense if you think about it.
		banBlid(%client.bl_id, 15, "AUTOBAN- TOO STUPID TO USE THE /SETNAME FUNCTION PROPERLY");
	}
	
	// if(nameBan(%first SPC %last)) //This bans you if your name is stupid like Ben Dover.
	// {
		// banBlid(%client.bl_id, 15, "AUTOBAN- NICE TRY " @ strUpr(%first SPC %last) @ "!");
		// return;
	// }

	if(!$RPChat::Clientdata::SeenWarning[%client.bl_id])
	{
		$RPChat::Clientdata::SeenWarning[%client.bl_id] = 1;
		servercmdRPCinfo(%client);
		messageClient(%client,'',"\c2You can view this warning at any time by using \c3/RPCinfo\c2.");
	}
	export("$RPChat::*","config/server/RPChatSave.cs");
}

function RPChatNameStupidParse(%bl_id)
{
	//Get data
	%first = $RPChat::Clientdata::Firstname[%bl_id];
	%last = $RPChat::Clientdata::Lastname[%bl_id];
	%nick = $RPChat::Clientdata::Nickname[%bl_id];
	//Remove < and >
	%first = strReplace(strReplace(%first, "<", ""), ">", "");
	%last = strReplace(strReplace(%last, "<", ""), ">", "");
	if(%nick !$= "")
		%nick = strReplace(strReplace(%nick, "<", ""), ">", "");
	//Capitalize the first letter, sorry ka'Chuck.
	%first = strUpr(getSubStr(%first, 0, 1)) @ getSubStr(%first, 1, strLen(%first));
	%last = strUpr(getSubStr(%last, 0, 1)) @ getSubStr(%last, 1, strLen(%last));
	if(%nick !$= "")
		%nick = strUpr(getSubStr(%nick, 0, 1)) @ getSubStr(%nick, 1, strLen(%nick));
	//Save data
	$RPChat::Clientdata::Firstname[%bl_id] = %first;
	$RPChat::Clientdata::Lastname[%bl_id] = %last;
	$RPChat::Clientdata::Nickname[%bl_id] = %nick;
}

function strInStr(%str)
{
	%parseStr = "fuck shit crap balls nigger badass";
	%words = getWordCount(%parseStr);
	for(%i = 0; %i < %words; %i++)
	{
		%word = getWord(%parseStr, %i);
		if(%str $= %word)
			return 1;
	}
	return 0;
}

function serverCmdWhoIs(%client, %name)
{
	if(%name $= "")
	{
		for(%i = 0; %i < ClientGroup.getCount(); %i++)
		{
			%cl = ClientGroup.getObject(%i);
			messageClient(%client, '', '\c6%1\c2 is also known as \c6%2\c2.', %cl.getRPName(), %cl.getPlayerName());
		}
		return;
	}
	for(%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if(strstr(%cl.getRPName(), %name) > -1)
		{
			messageClient(%client, '', '\c6%1\c2 is also known as \c6%2\c2.', %cl.getRPName(), %cl.getPlayerName());
			return;
		}
		if(strstr(%cl.getPlayerName(), %name) > -1)
		{
			messageClient(%client, '', '\c6%1\c2 is also known as \c6%2\c2.', %cl.getRPName(), %cl.getPlayerName());
			return;
		}
	}
	messageClient(%client, '', 'Could not find \c6\"%1\"\c0.', %name);
}

function serverCmdToggleOoc(%c, %a)
{
	if(%c.isSuperAdmin)
	{
		messageClient(%c, '', "Sorry bub, you're on the job!");
		return;
	}
	if(%a !$= "")
		%c.listeningtoooc = %a;
	else
		%c.listeningtoooc = !%c.listeningtoooc;
	messageClient(%c, '', "\c3You are no" @ (%c.listeningtoooc ? "w" : " longer") SPC "listening to the OOC channel.");
	for(%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if(%cl.listeningtoooc)
		{
			MessageClient(%cl, '', "\c3" @ %c.name SPC "can no" @ (%c.listeningtoooc ? "w" : " longer") SPC "hear OOC.");
		}
	}
	return %c.getPlayerName() SPC "is now deaf to the crys of his peers";
}

function servercmdtoggleradio(%c, %a)
{
	messageClient(%c, '', "\c3You turn" SPC (%c.listeningtoradio ? "on" : "off") SPC "your radio.");
	if(%a !$= "")
		%c.listeningtoradio = %a;
	else
		%c.listeningtoradio = !%c.listeningtoradio;
	return;
}

function serverCmdCarry(%c, %t)
{
	if(!isobject(%t = fcbn(%t)))
		return;
		
	if(vectorDist(%t.player.getPosition(), %c.player.getPosition()) > 5)
	{
		messageClient(%c, '', "\c3You are too far away to carry that person!");
		return;
	}
	
	if(isObject(%c.player.getMountedObject(0)))
	{
		messageClient(%c, '', "Your arms are full! Set down the person you're currently carrying with /drop first!");
		return;
	}
	
	%t.carryRequested = %c.bl_id;
	
	BluzoneLog(%c.name SPC "is trying to carry" SPC %t.name @ "!");
	
	messageClient(%c, '', "\c3You have invited " @ %t.name @ " into your arms.");
	messageClient(%t, '', "\c3" @ %c.name SPC "\c6is trying to pick you up!");
	messageClient(%t, '', "\c3Use \c2/acceptCarry\c3 or \c0/declineCarry\c3 to accept or decline the carry request!");
}

function serverCmdDrop(%c)
{
	if(isObject(%obj = %c.player.getMountedObject(0)))
	{
		%c.player.isholding = 0;
		%obj.dismount();
		%obj.playthread(0, "root");
		%c.player.playthread(0, "root");
	}
	serverCmdMessageSent(%c, "^sets down" SPC %obj.client.getRPName());
}

function serverCmdAcceptCarry(%t)
{
	%c = findClientByBL_ID(%t.carryRequested);
	if(vectorDist(%t.player.getPosition(), %c.player.getPosition()) > 5)
	{
		messageClient(%t, '', "\c3You are too far away for that person to pick you up!");
		return;
	}
	messageClient(%c, '', "\c3Your carry invitiation has been accepted! Use /drop to set your cargo down!");
	if(!isobject(%c.player.getMountedObject(0)))
	{
		%c.player.playThread(0, "armreadyBoth");
		%c.player.mountObject(%t.player, 0);
		%t.player.playThread(0, "death1");
		%c.player.isholding = 1;
		serverCmdMessageSent(%c, "^picks up" SPC %t.getRPName());
	}
}

function serverCmdDeclineCarry(%t)
{
	%c = findClientByBL_ID(%t.carryRequested);
	messageClient(%c, '', "\c3Your carry invitation has been declined.");
	messageClient(%t, '', "\c3You decline the carry invitation.");
	%t.carryRequested = "";
}

function servercmdRPCinfo(%client)
{
	messageClient(%client, '', '\c2Listen up kids, this stuff\'s \c6important\c2.', %client.getRPName());  
	messageClient(%client, '', '\c2Use regular chat to speak within a local range, 20 studs. Use team chat to shout globally.', %client.getRPName());
	messageClient(%client, '', '\c2Place the \c6^\c2 character while talking normally to send it as an action.', %client.getRPName());
	messageClient(%client, '', '\c2Place the \c6^\c2 character while shouting to send it as an \c6Out of Character\c2 message.', %client.getRPName());
	messageClient(%client, '', '\c2Place the \c6>\c2 character while talking normally to send it as a whisper, which reaches 6 studs in any direction.', %client.getRPName());
	messageClient(%client, '', '\c2Remember, you can use the command \c6/whoIs\c2 to find out the identity of another Role Player.', %client.getRPName());
}

function serverCmdStatus(%client, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19, %a20, %a21, %a22, %a23, %a24)
{
	%msg = %a1 SPC %a2 SPC %a3 SPC %a4 SPC %a5 SPC %a6 SPC %a7 SPC %a8 SPC %a9 SPC %a10 SPC %a11 SPC %a12 SPC %a13 SPC %a14 SPC %a15 SPC %a16 SPC %a17 SPC %a18 SPC %a19 SPC %a20 SPC %a21 SPC %a22 SPC %a23 SPC %a24;
	%msg = stripTrailingSpaces(%msg);
	%msg = MaxMessageLen(%msg);
	%client.rpStatus = %msg;
}