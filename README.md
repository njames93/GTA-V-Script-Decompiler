# V/RDR Script-Decompiler
A **command-line tool** that will decompile the script resources (xsc,csc,ysc,osc) files from the X360, PS3, PS4 and PC versions of Grand Theft Auto V and Red Dead Redemption 2 (RDR3). You are not allowed to use this tool for the purpose of creating modification in  GTA:Online/RDR:Online.

# Sources
1. [GTA-V-Script-Decompiler](https://github.com/zorg93/GTA-V-Script-Decompiler)
2. [gta5-nativedb-data](https://github.com/alloc8or/gta5-nativedb-data): Native table used for V & V-Console decompilation. [Dataset](GTA%20V%20Script%20Decompiler/Resources/natives.json) slightly modified to include cross-mapping. 32-bit console hashes use only the v323 Jenkins hashes.
3. [rdr3-nativedb-data](https://github.com/alloc8or/rdr3-nativedb-data): Native table used for RDR3 & RDR-Console decompilation. [Dataset](GTA%20V%20Script%20Decompiler/Resources/natives-rdr.json) will require modification on version bump & hash changes.

# Dependencies
1. [Newtonsoft.Json 12.0.3](https://www.nuget.org/packages/Newtonsoft.Json/)
2. [CommandLineParser 2.6.0](https://www.nuget.org/packages/CommandLineParser/)

# Options
```
  --help             Display this help screen.
  --version          Display version information.
  -i, --in           Input Directory/File Path.
  -o, --out          Output Directory/File Path.
  -c, --opcode       Opcode Set (v|vconsole|rdr|rdrconsole) (Default: v)
  -n, --natives      native json file. On null use the use the defined opcode set to select a native resource.
  -f, --force        Allow output file overriding. (Default: false)
  -a, --aggregate    Compute aggregation statistics of bulk dataset. (Default: false)
  --minlines         Minimum function line count for aggregation (Default: -1)
  --minhits          Minimum number of occurrences for aggregation. (Default: -1)
```

## Examples ## 

```sh
# Decompile a single RDR Native Script
decompiler.exe --opcode=rdr -n "Root:\rdr3-nativedb-data\natives.json" -i "Root:\rdr\ysc\startup_mp.ysc.full" -o "../somerelativepath/startup_mp.ysc.c"

# Bulk decompile GTA Native Scripts with aggregation statistics
decompiler.exe --opcode=v -a -n "Root:\gta5-nativedb-data\natives.json" -i "Root:\gta\ysc" -o "Root:\gta-v-decompiled-scripts.1737"
```

# Aggregation
A ScriptFile decompiles each function in a way that removes all state data. These stateless-functions are then compared (via SHA256) against the stateless-functions to all other scripts to create a collection of shared (aggregated) functions. For example,

```c
int af_intro_t_sandy.func_23(int iParam0)
{
	int iVar0;
	int iVar1;

	if (ENTITY::DOES_ENTITY_EXIST(iParam0))
	{
		iVar1 = ENTITY::GET_ENTITY_MODEL(iParam0);
		iVar0 = 0;
		while (iVar0 <= 2)
		{
			if (func_24(iVar0) == iVar1)
			{
				return iVar0;
			}
			iVar0++;
		}
	}
	return 145;
}
```

Stateless:

```c
int func_(Param)
{
	int i;
	int i;

	if (ENTITY::DOES_ENTITY_EXIST(Param))
	{
		Var = ENTITY::GET_ENTITY_MODEL(Param);
		Var = 0;
		while (Var <= 2)
		{
			if (func_(Var) == Var)
			{
				return Var;
			}
			Var++;
		}
	}
	return 145;
}
```

Which exists in 615 other locations across the v1737 GTA V PC natives.
