# n64aps
A patching tool for N64 roms.

# Requirements

[.Net Runtime 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)

# Usage

$ ./n64aps.exe

```
n64aps

Usage:
  n64aps [options] [command]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  single  Process single rom and/or patch
  multi   Process multiple roms and/or patches
```

```
single
  Process single rom and/or patch

Usage:
  n64aps [options] single [command]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  create  Create patch
  apply   Apply patch
  rename  Rename patch (CRC HI)
```

```
multi
  Process multiple roms and/or patches

Usage:
  n64aps [options] multi [command]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  create  Create patches
  apply   Apply patches
  rename  Rename patches (CRC HI)
```
