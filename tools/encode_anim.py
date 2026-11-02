# encode_anim.py <frames dir> <out dir> [scene ...]
# The frames of TheNorns.exe --demo-anim (<frames>\<scene>\0000.png, 25 fps, transparent around the body) as the site's
# clips, per scene:
#   <scene>.webp       animated WebP with alpha, loops: drops into an <img> on any background
#   <scene>.mp4        H.264 on the site's background (#07090e), for <video autoplay muted loop playsinline>
#   <scene>.webm       VP9 with alpha, the <video>'s first source where the browser plays it
#   <scene>-poster.jpg the first frame on the site's background, the <video>'s poster
import os, subprocess, sys

BG = "0x07090e"
FPS = 25


def run(args):
    subprocess.run(["ffmpeg", "-v", "error", "-y"] + args, check=True)


def encode(frames, out, scene):
    src = os.path.join(frames, scene, "%04d.png")
    even = "pad=ceil(iw/2)*2:ceil(ih/2)*2:color=0x00000000"   # 4:2:0 wants even sizes
    o = os.path.join(out, scene)
    run(["-framerate", str(FPS), "-i", src, "-vf", even, "-c:v", "libwebp_anim", "-lossless", "0", "-q:v", "82",
         "-compression_level", "6", "-loop", "0", "-pix_fmt", "yuva420p", o + ".webp"])
    flat = f"[0:v]{even}[fg];color=c={BG}:s=16x16:r={FPS}[bg];[bg][fg]scale2ref[bg2][fg2];[bg2][fg2]overlay=shortest=1,format=yuv420p"
    run(["-framerate", str(FPS), "-i", src, "-filter_complex", flat, "-c:v", "libx264", "-crf", "20",
         "-preset", "slow", "-movflags", "+faststart", o + ".mp4"])
    run(["-framerate", str(FPS), "-i", src, "-vf", even, "-c:v", "libvpx-vp9", "-pix_fmt", "yuva420p", "-b:v", "0",
         "-crf", "32", "-row-mt", "1", o + ".webm"])
    run(["-i", src, "-filter_complex", flat, "-frames:v", "1", "-q:v", "3", o + "-poster.jpg"])


def main():
    frames, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    scenes = sys.argv[3:] or sorted(d for d in os.listdir(frames) if os.path.isdir(os.path.join(frames, d)))
    for s in scenes:
        encode(frames, out, s)
        sizes = ", ".join(f"{ext} {os.path.getsize(os.path.join(out, s + ext)) // 1024} KB" for ext in (".webp", ".mp4", ".webm"))
        print(f"{s}: {sizes}")


if __name__ == "__main__":
    main()
