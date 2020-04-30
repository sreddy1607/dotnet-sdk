#!/usr/bin/env bash

set -e

usage()
{
    echo "Usage: $0 [BuildArch] [CodeName] [lldbx.y] [--skipunmount] --rootfsdir <directory>]"
    echo "BuildArch can be: arm(default), armel, arm64, x86"
    echo "CodeName - optional, Code name for Linux, can be: trusty, xenial(default), zesty, bionic, alpine. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen."
    echo "                              for FreeBSD can be: freebsd11 or freebsd12."
    echo "lldbx.y - optional, LLDB version, can be: lldb3.9(default), lldb4.0, lldb5.0, lldb6.0 no-lldb. Ignored for alpine and FReeBSD"
    echo "--skipunmount - optional, will skip the unmount of rootfs folder."
    exit 1
}

__CodeName=xenial
__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__InitialDir=$PWD
__BuildArch=arm
__AlpineArch=armv7
__QEMUArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__LLDB_Package="liblldb-3.9-dev"
__SkipUnmount=0

# base development support
__UbuntuPackages="build-essential"

__AlpinePackages="alpine-base"
__AlpinePackages+=" build-base"
__AlpinePackages+=" linux-headers"
__AlpinePackagesEdgeCommunity=" lldb-dev"
__AlpinePackagesEdgeMain=" llvm10-libs"
__AlpinePackagesEdgeMain+=" python3"
__AlpinePackagesEdgeMain+=" libedit"

# symlinks fixer
__UbuntuPackages+=" symlinks"

# CoreCLR and CoreFX dependencies
__UbuntuPackages+=" libicu-dev"
__UbuntuPackages+=" liblttng-ust-dev"
__UbuntuPackages+=" libunwind8-dev"

__AlpinePackages+=" gettext-dev"
__AlpinePackages+=" icu-dev"
__AlpinePackages+=" libunwind-dev"
__AlpinePackages+=" lttng-ust-dev"

# CoreFX dependencies
__UbuntuPackages+=" libcurl4-openssl-dev"
__UbuntuPackages+=" libkrb5-dev"
__UbuntuPackages+=" libssl-dev"
__UbuntuPackages+=" zlib1g-dev"

__AlpinePackages+=" curl-dev"
__AlpinePackages+=" krb5-dev"
__AlpinePackages+=" openssl-dev"
__AlpinePackages+=" zlib-dev"

__FreeBSDBase="12.1-RELEASE"
__FreeBSDPkg="1.12.0"
__FreeBSDPackages="libunwind"
__FreeBSDPackages+=" icu"
__FreeBSDPackages+=" libinotify"
__FreeBSDPackages+=" lttng-ust"
__FreeBSDPackages+=" krb5"

__UnprocessedBuildArgs=
while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm)
            __BuildArch=arm
            __UbuntuArch=armhf
            __AlpineArch=armv7
            __QEMUArch=arm
            ;;
        arm64)
            __BuildArch=arm64
            __UbuntuArch=arm64
            __AlpineArch=aarch64
            __QEMUArch=aarch64
            ;;
        armel)
            __BuildArch=armel
            __UbuntuArch=armel
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __CodeName=jessie
            ;;
        x86)
            __BuildArch=x86
            __UbuntuArch=i386
            __UbuntuRepo="http://archive.ubuntu.com/ubuntu/"
            ;;
        lldb3.6)
            __LLDB_Package="lldb-3.6-dev"
            ;;
        lldb3.8)
            __LLDB_Package="lldb-3.8-dev"
            ;;
        lldb3.9)
            __LLDB_Package="liblldb-3.9-dev"
            ;;
        lldb4.0)
            __LLDB_Package="liblldb-4.0-dev"
            ;;
        lldb5.0)
            __LLDB_Package="liblldb-5.0-dev"
            ;;
        lldb6.0)
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        no-lldb)
            unset __LLDB_Package
            ;;
        trusty) # Ubuntu 14.04
            if [ "$__CodeName" != "jessie" ]; then
                __CodeName=trusty
            fi
            ;;
        xenial) # Ubuntu 16.04
            if [ "$__CodeName" != "jessie" ]; then
                __CodeName=xenial
            fi
            ;;
        zesty) # Ubuntu 17.04
            if [ "$__CodeName" != "jessie" ]; then
                __CodeName=zesty
            fi
            ;;
        bionic) # Ubuntu 18.04
            if [ "$__CodeName" != "jessie" ]; then
                __CodeName=bionic
            fi
            ;;
        jessie) # Debian 8
            __CodeName=jessie
            __UbuntuRepo="http://ftp.debian.org/debian/"
            ;;
        stretch) # Debian 9
            __CodeName=stretch
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        buster) # Debian 10
            __CodeName=buster
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        tizen)
            if [ "$__BuildArch" != "armel" ]; then
                echo "Tizen is available only for armel."
                usage;
                exit 1;
            fi
            __CodeName=
            __UbuntuRepo=
            __Tizen=tizen
            ;;
        alpine)
            __CodeName=alpine
            __UbuntuRepo=
            ;;
        freebsd11)
            __FreeBSDBase="11.3-RELEASE"
            ;&
        freebsd12)
            __CodeName=freebsd
            __BuildArch=x64
            __SkipUnmount=1
            ;;
        --skipunmount)
            __SkipUnmount=1
            ;;
        --rootfsdir|-rootfsdir)
            shift
            __RootfsDir=$1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

if [ "$__BuildArch" == "armel" ]; then
    __LLDB_Package="lldb-3.5-dev"
fi
__UbuntuPackages+=" ${__LLDB_Package:-}"

if [ -z "$__RootfsDir" ] && [ ! -z "$ROOTFS_DIR" ]; then
    __RootfsDir=$ROOTFS_DIR
fi

if [ -z "$__RootfsDir" ]; then
    __RootfsDir="$__CrossDir/../../../.tools/rootfs/$__BuildArch"
fi

if [ -d "$__RootfsDir" ]; then
    if [ $__SkipUnmount == 0 ]; then
        umount $__RootfsDir/* || true
    fi
    rm -rf $__RootfsDir
fi

if [[ "$__CodeName" == "alpine" ]]; then
    __ApkToolsVersion=2.9.1
    __AlpineVersion=3.9
    __ApkToolsDir=$(mktemp -d)
    wget https://github.com/alpinelinux/apk-tools/releases/download/v$__ApkToolsVersion/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz -P $__ApkToolsDir
    tar -xf $__ApkToolsDir/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz -C $__ApkToolsDir
    mkdir -p $__RootfsDir/usr/bin
    cp -v /usr/bin/qemu-$__QEMUArch-static $__RootfsDir/usr/bin

    $__ApkToolsDir/apk-tools-$__ApkToolsVersion/apk \
      -X http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/main \
      -X http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/community \
      -U --allow-untrusted --root $__RootfsDir --arch $__AlpineArch --initdb \
      add $__AlpinePackages

    $__ApkToolsDir/apk-tools-$__ApkToolsVersion/apk \
      -X http://dl-cdn.alpinelinux.org/alpine/edge/main \
      -U --allow-untrusted --root $__RootfsDir --arch $__AlpineArch --initdb \
      add $__AlpinePackagesEdgeMain

    $__ApkToolsDir/apk-tools-$__ApkToolsVersion/apk \
      -X http://dl-cdn.alpinelinux.org/alpine/edge/community \
      -U --allow-untrusted --root $__RootfsDir --arch $__AlpineArch --initdb \
      add $__AlpinePackagesEdgeCommunity

    rm -r $__ApkToolsDir
elif [[ "$__CodeName" == "freebsd" ]]; then
    mkdir -p $__RootfsDir/usr/local/etc
    wget -O - https://download.freebsd.org/ftp/releases/amd64/${__FreeBSDBase}/base.txz | tar -C $__RootfsDir -Jxf - ./lib ./usr/lib ./usr/libdata ./usr/include ./usr/share/keys ./etc ./bin/freebsd-version
    # For now, ask for 11 ABI even on 12. This can be revisited later.
    echo "ABI = \"FreeBSD:11:amd64\"; FINGERPRINTS = \"${__RootfsDir}/usr/share/keys\"; REPOS_DIR = [\"${__RootfsDir}/etc/pkg\"]; REPO_AUTOUPDATE = NO; RUN_SCRIPTS = NO;" > ${__RootfsDir}/usr/local/etc/pkg.conf
    echo "FreeBSD: { url: "pkg+http://pkg.FreeBSD.org/\${ABI}/quarterly", mirror_type: \"srv\", signature_type: \"fingerprints\", fingerprints: \"${__RootfsDir}/usr/share/keys/pkg\", enabled: yes }" > ${__RootfsDir}/etc/pkg/FreeBSD.conf
    mkdir -p $__RootfsDir/tmp
    # get and build package manager
    wget -O -  https://github.com/freebsd/pkg/archive/${__FreeBSDPkg}.tar.gz  |  tar -C $__RootfsDir/tmp -zxf -
    cd $__RootfsDir/tmp/pkg-${__FreeBSDPkg}
    # needed for install to succeed
    mkdir -p $__RootfsDir/host/etc
    ./autogen.sh && ./configure --prefix=$__RootfsDir/host && make && make install
    rm -rf $__RootfsDir/tmp/pkg-${__FreeBSDPkg}
    # install packages we need.
    INSTALL_AS_USER=$(whoami) $__RootfsDir/host/sbin/pkg -r $__RootfsDir -C $__RootfsDir/usr/local/etc/pkg.conf update
    INSTALL_AS_USER=$(whoami) $__RootfsDir/host/sbin/pkg -r $__RootfsDir -C $__RootfsDir/usr/local/etc/pkg.conf install --yes $__FreeBSDPackages
elif [[ -n $__CodeName ]]; then
    qemu-debootstrap --arch $__UbuntuArch $__CodeName $__RootfsDir $__UbuntuRepo
    cp $__CrossDir/$__BuildArch/sources.list.$__CodeName $__RootfsDir/etc/apt/sources.list
    chroot $__RootfsDir apt-get update
    chroot $__RootfsDir apt-get -f -y install
    chroot $__RootfsDir apt-get -y install $__UbuntuPackages
    chroot $__RootfsDir symlinks -cr /usr

    if [ $__SkipUnmount == 0 ]; then
        umount $__RootfsDir/* || true
    fi

    if [[ "$__BuildArch" == "arm" && "$__CodeName" == "trusty" ]]; then
        pushd $__RootfsDir
        patch -p1 < $__CrossDir/$__BuildArch/trusty.patch
        patch -p1 < $__CrossDir/$__BuildArch/trusty-lttng-2.4.patch
        popd
    fi
elif [ "$__Tizen" == "tizen" ]; then
    ROOTFS_DIR=$__RootfsDir $__CrossDir/$__BuildArch/tizen-build-rootfs.sh
else
    echo "Unsupported target platform."
    usage;
    exit 1
fi
