import zipfile, re, os
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
z=zipfile.ZipFile(p)
for n in z.namelist():
    if n.startswith("xl/drawings/") and n.endswith(".xml"):
        data=z.read(n).decode("utf-8")
        texts=re.findall(r"<a:t>(.*?)</a:t>", data, re.S)
        print("==",n,len(data),"texts:",len(texts))
        print(" | ".join(texts))
