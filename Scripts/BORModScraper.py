import xml.dom.minidom as xml
import pandas as pd
import os

path = "xml\\"
out_path = "data.csv"
debug_msg = True

def debug(message):
    if debug_msg:
        print(message)

def getChildByChildrenTagName(node, name):
    nlist = []
    for child in node.childNodes:
        for child2 in child.childNodes:
            if child2.nodeType == xml.Node.ELEMENT_NODE and child2.tagName == name:
                nlist.append(child)
    return nlist
            

def all_files_in_dir(dirPath = path):
    if dirPath == path:
        debug("scanning directory xml\\ for files...")
    flist = []
    for dir_item in os.scandir(dirPath):
        if dir_item.is_file() and dir_item.path[-4:] == ".xml":
            debug("file found at path: " + dir_item.path)
            flist.append(dir_item.path)
        elif dir_item.is_dir():
            flist.extend(all_files_in_dir(dir_item.path))
    if dirPath == path:
        debug(f'Found {len(flist)} files to process')
    return flist

def process_files():
    data = {"type": [], "name": [], "label": [], "desc": [], "file_name": []}
    for file in all_files_in_dir():
        process_file(data, file)
    pd.DataFrame(data).to_csv(out_path)
    debug(f'data output to {out_path}')

def process_file(data, file):
    debug(f'processing file {file}')
    dom = xml.parse(file).firstChild
    elements = getChildByChildrenTagName(dom, "description")
    debug(f'found {len(elements)} nodes to process')
    for element in elements:
        if not element.hasAttribute("Abstract"):
            data["type"].append(element.tagName)
            data["name"].append(element.getElementsByTagName("defName")[0].childNodes[0].data)
            data["label"].append(element.getElementsByTagName("label")[0].childNodes[0].data)
            data["desc"].append(element.getElementsByTagName("description")[0].childNodes[0].data)
            data["file_name"].append(file)

if __name__ == "__main__":
    process_files()
