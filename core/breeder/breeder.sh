if [ "$EUID" -ne 0 ]
  then echo "Iterkocze Breeder makes use of 'mount'. You need to be root in order to use it"
  exit 1
fi

echo "Iterkocze Breeder 1.0"

userName="$(id -un 1000)"
tarball="/home/${userName}/iterkoczeos.tar.xz"

if [[ ! -f "${tarball}" ]]; then
	echo "Can't find OS release in ${tarball}"
	exit 1
fi

if ! command -v cpio &> /dev/null
then
	echo "cpio not found. Install package 'cpio' using Iterkocze Paka"
	exit 1
fi

if ! command -v advdef &> /dev/null
then
        echo "advdef not found. Install package 'advancecomp' using Iterkocze Paka"
        exit 1
fi

if ! command -v mkisofs &> /dev/null
then
        echo "mkisofs not found. Install package 'cdrtools' using Iterkocze Paka"
        exit 1
fi

cd /programs/system/breeder
mkdir -p data
cd data
if [[ ! -f "CorePure64-14.0.iso" ]]; then
	echo "Tiny Core iso not found. Downloading..."
	wget http://tinycorelinux.net/14.x/x86_64/release/CorePure64-14.0.iso
fi

echo "Unpacking ISO..."

mkdir -p mounted
mkdir -p raw
mkdir -p ext
mkdir -p newiso/boot
mount CorePure64-14.0.iso ./mounted -o loop,ro
cp -a ./mounted/boot ./raw
umount ./mounted

echo "Done"

cd ext
zcat ../raw/boot/corepure64.gz | cpio -i -u -H newc -d
cp -v -u "${tarball}" ./
cp -v -u /programs/system/breeder/os-install ./

echo "Writing new core.gz..."

find | cpio -o -H newc | gzip -2 > ../newiso/boot/corepure64.gz

echo "Done"
echo "Compressing new core..."
advdef -z0 ../newiso/boot/corepure64.gz

echo "Done"
echo "Writing ISO..."

cd ../newiso/boot
cp -v -u ../../raw/boot/vmlinuz64 ./
cp -r ../../raw/boot/isolinux ./

cd ../../

cp -r ./raw/boot/isolinux ./

mkisofs -l -J -R -V TC-custom -no-emul-boot -boot-load-size 4 -boot-info-table -b boot/isolinux/isolinux.bin -c boot/isolinux/boot.cat -o IterkoczeOS.iso newiso


mv -v IterkoczeOS.iso "/home/${userName}/IterkoczeOS.iso"
echo "ISO file ready. You can find it in your home directory"
echo "/programs/system/breeder/data can be safely deleted"
