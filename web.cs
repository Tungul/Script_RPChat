////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//  @title    Torque Webserver                                                //
//  @author   Truce                                                           //
//  @version  Beta 2.1                                                        //
//                                                                            //
//  Dynamically writes pages created by the user by parsing raw Torquescript  //
//  located between the <?tqs and ?> tags with support for both GET and POST  //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

exec("config/randompick.cs");

if(isObject(Webserver))
	Webserver.delete();

// if(isObject(WebSocketClients))
// {
// 	echo("Found WebSocket clients list. Deleting.");
// 	for(%i = 0; %i < WebSocketClients.getCount(); %i++)
// 	{
// 		%c = WebSocketClients.getObject(%i);
// 		%c.schedule(5000, delete);
// 	}
// }

if(!isObject(Webserver))
{
	new TCPObject(Webserver)
	{
		port      = 33580;
		debug     = false;
		localOnly = false;
		localIPs  = "127.0.0.1 192.168. 10.0.";
		blockIPs  = "";
		timeout   = 500;
		accounts  = "config/accounts.dat";
		folder    = "config/pages";
		index     = "/index.tqs";
		tagPrefix = "<?tqs ";
		tagSuffix = " ?>";
	};
	
	Webserver.listen(Webserver.port);
}

function Webserver::debug(%this,%line)
{
	if(%this.debug)
		echo("[Webserver] " @ %line);
}

function Webserver::onConnectRequest(%this,%address,%id)
{
	%address       = getWord(strReplace(%address,":"," "),1);
	%client        = new TCPObject(Webclient,%id).getID();
	%client.ip     = %address;
	%client.server = %this;
	
	%this.debug("Connect request from IP " @ %address @ " (" @ %client @ ")");
	
	if(%this.localOnly)
	{
		%localIPs = %this.localIPs;
		%count    = getWordCount(%localIPs);
		
		for(%i = 0; %i < %count; %i++)
		{
			%localIP = getWord(%localIPs,%i);
			
			if(strPos(%address,%localIP) == 0)
				break;
		}
		
		if(%i == %count)
		{
			%this.debug("> Client is not local and will be disconnected.");
			%client.disconnect();
			
			return;
		}
	}
	
	%blockIPs = %this.blockIPs;
	%count    = getWordCount(%blockIPs);
	
	for(%i = 0; %i < %count; %i++)
	{
		%blockIP = getWord(%blockIPs,%i);
		
		if(strPos(%address,%blockIP) == 0)
		{
			%this.debug("> Client is on blacklist and will be disconnected.");
			%client.disconnect();
			
			return;
		}
	}
	%timeout        = %this.timeout;
	%client.timeout = %client.schedule(%timeout,timeout);
	
	%this.debug("> Client timeout in " @ %timeout @ " milliseconds.");
}

function Webclient::timeout(%this)
{
	%this.server.debug("Client " @ %this @ " timed out after some time.");
	%this.disconnect();
	%this.delete();
}

function Webclient::onDisconnect(%this)
{
	%this.delete();
}

function Webclient::onLine(%this,%line)
{
	cancel(%this.timeout);

	if(%this.isWebSocket)
	{
		echo("found websocket line");
		%this.onWebSocketLine(%line);
		return;
	}
	
	%this.timeout = %this.schedule(Webserver.timeout,finish);
	%this.packet  = %this.packet @ %line @ "\n";
	%length       = %this._SERVER["HTTP_CONTENT_LENGTH"];
	
	if(!%this.lineCount)
	{
		%this.command = getWord(%line,0);
		%this.page    = getWord(%line,1);
		%this.version = getWord(%line,2);
	}
	else if(%line $= "" && %length !$= "")
	{
		%this.binarySize = %length;
		%this.setBinary(1);
	}
	else
	{
		%args  = strReplace(%line,": ","\t");
		%name  = "HTTP_" @ strReplace(getField(%args,0),"-","_");
		%value = getField(%args,1);
		
		%this._SERVER[%name] = %value;
	}
	
	%this.lineCount++;
}
// webserver.debug = 1;

function Webclient::onBinChunk(%this,%chunk)
{
	if(%chunk >= %this.binarySize)
		%this.saveBufferToFile("config/temp");
	
	cancel(%this.timeout);
	%this.timeout = %this.schedule(Webserver.timeout,finish);
}

function Webclient::finish(%this)
{
	%server  = %this.server;
	%packet  = %this.packet;
	%request = getRecord(%packet,0);
	
	if(isFile("config/temp"))
	{
		%file = new FileObject();
		%file.openForRead("config/temp");
		
		while(!%file.isEOF())
			%packet = %packet @ %file.readLine() @ "\n";
		
		%file.close();
		%file.delete();
		
		fileDelete("config/temp");
	}
	
	%server.debug("Packet terminated from client " @ %this @ ".");
	
	%command = getWord(%request,0);
	%page    = getWord(%request,1);
	%version = getWord(%request,2);
	
	if(%version !$= "HTTP/1.1")
	{
		%server.debug("> Client using old HTTP and will be disconnected."); 
		%this.disconnect();
		
		return;
	}
	
	deleteVariables("$_GET*");
	deleteVariables("$_POST*");
	deleteVariables("$_SERVER*");
	
	$_SERVER["REMOTE_ADDR"] = %this.ip;
	$_SERVER["URI"] = %request;
	
	if((%pos = strPos(%page,"?")) != -1)
	{
		%args = getSubStr(%page,%pos + 1,strLen(%page));
		%page = getSubStr(%page,0,%pos);
		
		%server.debug("Parsing client " @ %this @ "'s GET args: " @ %args);
		
		%args = strReplace(%args,"&","\t");
		%num  = getFieldCount(%args);
		
		if(!%num)
			%server.debug("> No GET args found to parse!");
		
		for(%i = 0; %i < %num; %i++)
		{
			%arg = getField(%args,%i);
			%arg = strReplace(%arg,"=","\t");
			
			%name  = getField(%arg,0);
			%value = getField(%arg,1);
			
			$_GET[%name] = %value;
			%server.debug("> Assigning " @ %value @ " to " @ %name @ ".");
		}
	}
	
	%header = getRecords(%packet,1);
	%count  = getRecordCount(%header);
	
	%server.debug("Parsing client " @ %this @ "'s header: " @ %header);
	
	for(%i = 0; %i < %count; %i++)
	{
		%record = getRecord(%header,%i);
		
		if(%record $= "")
		{
			%args = getRecord(%header,%i + 1);
			
			%server.debug("Parsing client " @ %this @ "'s POST args: " @ %args);
			
			%args = strReplace(%args,"&","\t");
			%num  = getFieldCount(%args);
			
			if(!%num)
				%server.debug("> No POST args found to parse!");
			
			for(%i = 0; %i < %num; %i++)
			{
				%arg = getField(%args,%i);
				%arg = strReplace(%arg,"=","\t");
				
				%name  = getField(%arg,0);
				%value = getField(%arg,1);
				
				$_POST[%name] = %value;
				%server.debug("> Assigning " @ %value @ " to " @ %name @ ".");
			}
			
			break;
		}
		
		%record = strReplace(%record,": ","\t");
		%name   = "HTTP_" @ strReplace(getField(%record,0),"-","_");
		%value  = getField(%record,1);
		
		$_SERVER[%name] = %value;
		%server.debug("> Assigning " @ %value @ " to " @ %name @ ".");
	}

	if($_SERVERHTTP_Connection $= "Upgrade" && $_SERVERHTTP_UPGRADE $= "websocket")
	{
		// recho("> Upgrading connection to websocket.");
		echo("> Upgrading connection to websocket.");

		%rkey = convertWebkey($_SERVERHTTP_Sec_WebSocket_Key @ "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

		// recho("sending string " @ %rkey);
		echo("sending string " @ %rkey);

		%this.send("HTTP/1.1 101 Switching Protocols\r\n");
		%this.send("Upgrade: websocket\r\n");
		%this.send("Connection: Upgrade\r\n");
		%this.send("Sec-WebSocket-Accept: " @ %rkey @ "\r\n");
		%this.send("\r\n");

		%this.isWebSocket = 1;

		if(!isObject(WebSocketClients))
			new SimSet(WebSocketClients);

		WebSocketClients.add(%this);

		return;
	}
	
	if(%page $= "/")
		%page = %server.index;

	%server.debug("Deploying file: " @ %page);
	
	if(fileExt(%server.folder @ %page) $= ".tqss")
	{
		if(!isFile(%server.accounts))
		{
			%server.debug("> Requested secured page, but no accounts file exists.");
			%this.disconnect();
			
			return;
		}
		
		if($_SERVER["HTTP_AUTHORIZATION"] $= "")
		{
			%server.debug("> Requested secured page, prompting for authentication.");
			
			%this.send("HTTP/1.1 401 Authorization Required\r\n");
			%this.send("WWW-Authenticate: Basic realm=\"TQS Webserver\"\r\n");
			%this.send("\r\n");
			
			%this.disconnect();
			return;
		}
		
		%auth = getWord($_SERVER["HTTP_AUTHORIZATION"],1);
		%auth = base64Decode(%auth);
		
		%file = new FileObject();
		%file.openForRead(%server.accounts);
		
		while(!%file.isEOF())
		{
			%line = %file.readLine();
			%pos  = strPos(%line,"#");
			
			if(%pos != -1)
				%line = getSubStr(%line,0,%pos);
			
			%line = trim(%line);
			
			if(%line !$= "" && %line $= %auth)
			{
				%authed = true;
				break;
			}
		}
		
		%file.close();
		%file.delete();
		
		if(!%authed)
		{
			%server.debug("> Client provided invalid credentials, access denied.");
			
			%this.send("HTTP/1.1 401 Authorization Required\r\n");
			%this.send("WWW-Authenticate: Basic realm=\"TQS Webserver\"\r\n");
			%this.send("\r\n");
			
			%this.disconnect();
			return;
		}
		
		%server.debug("> Client provided proper credentials, access granted.");
	}
	
	if(striPos(%page,"http://") != -1)
	{
		%server.debug("> Client tempting security and will be disconnected.");
		%this.disconnect();
		
		return;
	}

	if(%page $= "/status.tqs" && !%done)
	{
		%server.debug("> Deploying server status");
		
		if($Pref::Server::Password $= "")
		{
			%pic = "http://dl.dropbox.com/u/58130173/BluzoneImages/SERVEROPEN.png";
		}
		else
		{
			%pic = "http://dl.dropbox.com/u/58130173/BluzoneImages/SERVERCLOSED.png";
		}
		
		%this.send("HTTP/1.1 303 See Other\r\n");
		%this.send("Location: " @ %pic @ "\r\n");
		%this.send("Connection: close\r\n");
		%this.send("\r\n");
		%done = 1;
	}

	if(%page $= "/randomprofile.tqs" && !%done)
	{
		%server.debug("> Getting Random Profile");
		
		%page = "http://" @ "forum.blockland.us/index.php?action=profile;u=" @ getRandom(0, 36467);
		
		%this.send("HTTP/1.1 303 See Other\r\n");
		%this.send("Location: " @ %page @ "\r\n");
		%this.send("Connection: close\r\n");
		%this.send("\r\n");
		%done = 1;
	}

	if(%page $= "/random.tqs" && !%done)
	{
		%server.debug("> Displaying random image.");
		
		%img = randomPickimage("BluzoneImages/randomPick/");
		
		%this.send("HTTP/1.1 303 See Other\r\n");
		%this.send("Location: " @ %img @ "\r\n");
		%this.send("Connection: close\r\n");
		%this.send("\r\n");
		%done = 1;
	}

	if(%page $= "/lumberjack.tqs" && !%done)
	{
		%server.debug("> Displaying random image.");
		
		// %img = randomPickimage("BluzoneImages/randomPick/");
		%img = "http://dl.dropbox.com/u/58130173/BluzoneImages/randomPick/ebook.png";
		WSLog("lumberjack", "<td>" @ $_SERVERREMOTE_ADDR @ "</td><td>" @ $_GETS @ "</td><td>" @ $_SERVERHTTP_REFERER @ "</td>");

		%this.send("HTTP/1.1 303 See Other\r\n");
		%this.send("Location: " @ %img @ "\r\n");
		%this.send("Connection: close\r\n");
		%this.send("\r\n");
		%done = 1;
	}
	
	if(%page $= "/chatterbox.tqs" && !%done)
	{
		%forceDie = 1;
	}

	if(fileExt(%page) $= ".svg")
	{
		%type = "image/svg+xml;";
	}

	if(fileExt(%page) $= ".css")
	{
		%type = "text/css;";
	}

	if(!%done && isFile(%server.folder @ %page))
	{
		%server.debug("> File found! Including all its contents.");

		$Webkey = -1;
		%body   = input(%page);

		%this.send("HTTP/1.1 200 OK\r\n");
		%this.send("Content-Length: " @ strLen(%body) @ "\r\n");
		%this.send("Content-Type: " @ ((%type $= "") ? "text/html;" : %type) @ " charset=UTF-8\r\n");
		if(!%forceDie)
			%this.send("Connection: Keep-Alive\r\n");
		else
			%this.send("Connection: close\r\n");
		%this.send("\r\n");
		%this.send(%body @ "\r\n");
	}
	else
	{
		if(!%done)
		{
			%server.debug("> File not found! Deploying 404 instead.");
			
			%this.send("HTTP/1.1 404 Not Found\r\n");
			%this.send("Connection: close\r\n");
			%this.send("\r\n");
		}
	}
	
	if($Webserver::Views $= "")
		$Webserver::Views = 0;
	
	%this.schedule(400, disconnect);
	%this.schedule(1000, delete);
}

function Webclient::onWebSocketLine(%this, %line)
{
	echo("Websocket Received data:" @ %line);
	%this.send("Echo:" SPC %line);
}

function parse(%_body)
{
	%_pre  = Webserver.tagPrefix;
	%_suf  = Webserver.tagSuffix;
	%_pos  = striPos(%_body,%_pre);
	%_rest = getSubStr(%_body,%_pos + strLen(%_pre),strLen(%_body));
	%_body = getSubStr(%_body,0,%_pos);
	%_pos  = striPos(%_rest,%_suf);
	%_eval = getSubStr(%_rest,0,%_pos);
	%_rest = getSubStr(%_rest,%_pos + strLen(%_suf),strLen(%_rest));
	
	$Webcache[$Webkey] = "";
	eval(%_eval);
	
	return %_body @ $Webcache[$Webkey] @ %_rest;
}

function input(%path)
{
	%path = Webserver.folder @ %path;
	
	if(!isFile(%path))
		return;
	
	%file = new FileObject();
	%file.openForRead(%path);
	
	while(!%file.isEOF())
		%body = %body @ strReplace(%file.readLine(),"\t","    ") @ " \n ";
	
	%file.close();
	%file.delete();
	
	%body = getSubStr(%body,0,strLen(%body) - 3);
	%pre  = Webserver.tagPrefix;
	$Webkey++;
	
	while(striPos(%body,%pre) != -1)
		%body = parse(%body);
	
	$Webkey--;
	return %body;
}

function include(%path)
{
	print(input(%path));
}

function print(%str)
{
	$Webcache[$Webkey] = $Webcache[$Webkey] @ %str;
}

function puts(%str)
{
	print(%str @ "\n");
}

function WSLog(%name, %text)
{
	%date = strreplace(getsubstr(getDateTime(),0,8),"/","-");
	%time = getsubstr(getDateTime(),9,8);
	%file = new FileObject();
	if(!isFile("config/pages/logs/" @ %name @ ".tqss"))
		%newfile = 1;
	%file.openForAppend("config/pages/logs/" @ %name @ ".tqss");
	if(%newfile)
	{
		%file.writeLine("<table border=1 cellpadding=5 width=100%>");
	}
	%file.writeline("<tr><td>" @ %time @ "</td>" @ %text @ "</tr>");
	%file.close();
	%file.delete();
}

function getBannedFromIP(%ip)
{
	new fileObject(BanFindFO) { targetID = %ip; };
	BanFindFO.openForRead("config/server/BANLIST.txt");
	while(!BanFindFO.isEOF())
	{
		BanFindFO.line = BanFindFO.readLine();
		if(BanFindFO.targetid $= getField(BanFindFO.line, 4))
		{
			BanFindFO.close();
			BanFindFO.delete();
			return true;
		}
	}
	BanFindFO.close();
	BanFindFO.delete();
	return false;
}

function getIsAdminFromIP(%ip)
{
	%BL_ID = getBL_IDFromIP(%ip);
	if(!%BL_ID)
		return false;
	if(%BL_ID $= "16807")
		return true;
	if(strPos($Pref::Server::AutoAdminList, %BL_ID) > -1)
		return true;
	if(strPos($Pref::Server::AutoSuperAdminList, %BL_ID) > -1)
		return true;
	return false;
}

function getBL_IDFromIP(%ip)
{
	new fileObject(NameFindFO) { targetID = %ip; };
	%a = "config/server/adminlogs/connections.txt";
	%b = "config/server/adminlogs/connections00.txt";
	NameFindFO.openForRead(%a);
	while(!NameFindFO.isEOF())
	{
		NameFindFO.line = NameFindFO.readLine();
		if(NameFindFO.targetid $= getField(NameFindFO.line, 5))
		{
			%name = getField(NameFindFO.line, 4);
			NameFindFO.close();
			NameFindFO.delete();
			return %name;
		}
	}
	NameFindFO.close();
	if(!isFile(%b))
	{
		NameFindFO.delete();
		return false;
	}
	else
	{
		NameFindFO.openForRead(%b);
		while(!NameFindFO.isEOF())
		{
			NameFindFO.line = NameFindFO.readLine();
			if(NameFindFO.targetid $= getField(NameFindFO.line, 5))
			{
				%name = getField(NameFindFO.line, 4);
				NameFindFO.close();
				NameFindFO.delete();
				return %name;
			}
		}
		NameFindFO.close();
	}
	%name = getNameFromIP(%ip);
	if(%name !$= "0")
	{
		NameFindFO.openForRead(%a);
		while(!NameFindFO.isEOF())
		{
			NameFindFO.line = NameFindFO.readLine();
			if(%name $= getField(NameFindFO.line, 3))
			{
				%blid = getField(NameFindFO.line, 4);
				NameFindFO.close();
				NameFindFO.delete();
				return %blid;
			}
		}
		NameFindFO.close();
	}
	NameFindFO.delete();
	return false;
}

function getNameFromIP(%ip)
{
	if($Chatterbox::Exception[%ip] !$= "")
		return $Chatterbox::Exception[%ip];
	new fileObject(NameFindFO) { targetID = %ip; };
	%a = "config/server/adminlogs/connections.txt";
	%b = "config/server/adminlogs/connections00.txt";
	NameFindFO.openForRead(%a);
	while(!NameFindFO.isEOF())
	{
		NameFindFO.line = NameFindFO.readLine();
		if(NameFindFO.targetid $= getField(NameFindFO.line, 5))
		{
			%name = getField(NameFindFO.line, 3);
			NameFindFO.close();
			NameFindFO.delete();
			return %name;
		}
	}
	NameFindFO.close();
	if(!isFile(%b))
	{
		NameFindFO.delete();
		return 0;
	}
	else
	{
		NameFindFO.openForRead(%b);
		while(!NameFindFO.isEOF())
		{
			NameFindFO.line = NameFindFO.readLine();
			if(NameFindFO.targetid $= getField(NameFindFO.line, 5))
			{
				%name = getField(NameFindFO.line, 3);
				NameFindFO.close();
				NameFindFO.delete();
				return %name;
			}
		}
		NameFindFO.close();
		NameFindFO.delete();
		return 0;
	}
}

function findNameByIP(%ip) { return getNameFromIP(%ip); }

function randomPickImage(%folder, %small)
{
	%num = getRandom(0, $Web::randomPicker::bluzoneNum);
	%img = $Web::randomPicker::bluzone[%num];
	return "http://dl.dropbox.com/u/58130173/" @ %folder @ ((%small) ? "SM" : "") @ %img;
}

// function superSock(%rkey)
// {
// 	$SuperSock.send("Sec-WebSocket-Accept: " @ %rkey @ "\r\n");
// 	$SuperSock.send("\r\n");
// }

////////////////////////////////////////
//  Base64 Pack             by Truce  //
////////////////////////////////////////

function convertBase(%val,%atype,%btype)
{
	%vlen = strLen(%val);
	%alen = strLen(%atype);
	%blen = strLen(%btype);
	
	for(%i = 0; %i < %vlen; %i++)
		%sum += striPos(%atype,getSubStr(%val,%i,1)) * mPow(%alen,%vlen - %i - 1);
	
	while(1)
	{
		%rem = %sum % %blen;
		%new = getSubStr(%btype,%rem,1) @ %new;
		%sum = mFloor(%sum / %blen);
		
		if(!%sum)
			break;
	}
	
	return %new;
}

function base64Encode(%str)
{
   %base64map = "ABCDEFGHIJKLMNOPQRSTUVWXYZabc defghijklmnopqrstuvwxyz012345 6789+/";
   %asciimap  =     "\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F" @
                "\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F" @
                "\x20\x21\x22\x23\x24\x25\x26\x27\x28\x29\x2A\x2B\x2C\x2D\x2E\x2F" @
                "\x30\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3A\x3B\x3C\x3D\x3E\x3F" @
                "\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4A\x4B\x4C\x4D\x4E\x4F" @
                "\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5A\x5B\x5C\x5D\x5E\x5F" @
                "\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6A\x6B\x6C\x6D\x6E\x6F" @
                "\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7A\x7B\x7C\x7D\x7E\x7F" @
                "\x80\x81\x82\x83\x84\x85\x86\x87\x88\x89\x8A\x8B\x8C\x8D\x8E\x8F" @
                "\x90\x91\x92\x93\x94\x95\x96\x97\x98\x99\x9A\x9B\x9C\x9D\x9E\x9F" @
                "\xA0\xA1\xA2\xA3\xA4\xA5\xA6\xA7\xA8\xA9\xAA\xAB\xAC\xAD\xAE\xAF" @
                "\xB0\xB1\xB2\xB3\xB4\xB5\xB6\xB7\xB8\xB9\xBA\xBB\xBC\xBD\xBE\xBF" @
                "\xC0\xC1\xC2\xC3\xC4\xC5\xC6\xC7\xC8\xC9\xCA\xCB\xCC\xCD\xCE\xCF" @
                "\xD0\xD1\xD2\xD3\xD4\xD5\xD6\xD7\xD8\xD9\xDA\xDB\xDC\xDD\xDE\xDF" @
                "\xE0\xE1\xE2\xE3\xE4\xE5\xE6\xE7\xE8\xE9\xEA\xEB\xEC\xED\xEE\xEF" @
                "\xF0\xF1\xF2\xF3\xF4\xF5\xF6\xF7\xF8\xF9\xFA\xFB\xFC\xFD\xFE\xFF";
   
   %len = strLen(%str);
   
   for(%i = 0; %i < %len; %i++)
   {
      %chr   = getSubStr(%str,%i,1);
      %ascii = strPos(%asciimap,%chr) + 1;
      %bin   = convertBase(%ascii,"0123456789","01");
      
      while(strLen(%bin) < 8)
         %bin = "0" @ %bin;
      
      %all = %all @ %bin;
   }
   
   %len = strLen(%all);
   
   for(%i = 0; %i < %len; %i += 6)
   {
      %pack = getSubStr(%all,%i,6);
      
      while(strLen(%pack) < 6)
         %pack = %pack @ "0";
      
      %dec = convertBase(%pack,"01","0123456789");
      %new = %new @ getSubStr(%base64map,%dec,1);
   }
   
   while(strLen(%new) % 4 > 0)
      %new = %new @ "=";
   
   return %new;
}

function base64Decode(%str)
{
	%base64map = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
	%asciimap  = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMN" @
	             "OPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
	
	while(getSubStr(%str,strLen(%str) - 1,1) $= "=")
		%str = getSubStr(%str,0,strLen(%str) - 1);
	
	%len = strLen(%str);
	
	for(%i = 0; %i < %len; %i++)
	{
		%chr = getSubStr(%str,%i,1);
		%pos = strPos(%base64map,%chr);
		%bin = convertBase(%pos,"0123456789","01");
		
		while(strLen(%bin) < 6)
			%bin = "0" @ %bin;
		
		%all = %all @ %bin;
	}
	
	while(strLen(%all) % 8 > 0)
		%all = getSubStr(%all,0,strLen(%all) - 1);
	
	%len = strLen(%all);
	
	for(%i = 0; %i < %len; %i += 8)
	{
		%bin = getSubStr(%all,%i,8);
		%dec = convertBase(%bin,"01","0123456789") - 32;
		%chr = getSubStr(%asciiMap,%dec,1);
		
		%new = %new @ %chr;
	}
	
	return %new;
}

function convertWebkey(%key)
{
	%sha = sha1(%key);
	%len = strLen(%sha);

	for(%i = 0; %i < %len; %i += 2)
		%str = %str SPC convertBase(getSubStr(%sha, %i, 2), "0123456789ABCDEF", "0123456789");

	%str = trim(%str);

	%b64 = base64Encode_Nulls(%str);

	return %b64;
}

function base64Encode_Nulls(%str)
{
	%base64map = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+\/";

	%len = getWordCount(%str);

	for(%i = 0; %i < %len; %i++)
	{
		%ascii = getWord(%str, %i);

		%bin = convertBase(%ascii,"0123456789","01");

		while(strLen(%bin) < 8) %bin = "0" @ %bin;
		
		%all = %all @ %bin;
	}

	%len = strLen(%all);
	
	for(%i = 0; %i <%len; %i += 6)
	{
		%pack = getSubStr(%all, %i, 6);
	
		while(strLen(%pack) < 6) %pack = %pack @ "0";
		
		%dec = convertBase(%pack, "01", "0123456789");

		%new = %new @ getSubStr(%base64map, %dec, 1);
	}
	
	while(strLen(%new) % 4 > 0) %new = %new @ "=";
	
	return %new;
}