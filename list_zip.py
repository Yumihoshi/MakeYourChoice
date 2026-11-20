import zipfile
p=r"D:\Projects\UnityProjects\MakeYourChoice\docs\后室游戏交互节点_1-2 (1) (1).xlsx"
z=zipfile.ZipFile(p)
for n in z.namelist(): print(n)
