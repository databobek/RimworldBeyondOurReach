import xml.dom.minidom as xml
import pandas as pd
import os

path = "..\\Common\\Defs"
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

def getChildrenByTagName(node, name):
    nlist = []
    for child in node.childNodes:
        if child.nodeType == xml.Node.ELEMENT_NODE and child.tagName == name:
            nlist.append(child)
    return nlist

def all_files_in_dir(dirPath = path):
    if dirPath == path:
        debug(f"scanning directory {dirPath} for files...")
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

def scrape_files_for_descs(out_path):
    data = {"file_name": [], "type": [], "name": [], "label": [], "desc": []}
    for file in all_files_in_dir():
        scrape_file_for_descs(data, file)
    pd.DataFrame(data).to_csv(out_path)
    debug(f'data output to {out_path}')

def scrape_file_for_descs(data, file):
    debug(f'processing file {file}')
    dom = xml.parse(file).firstChild
    elements = getChildByChildrenTagName(dom, "description")
    debug(f'found {len(elements)} nodes to process')
    for element in elements:
        if not element.hasAttribute("Abstract"):
            data["file_name"].append(file)
            data["type"].append(element.tagName)
            data["name"].append(element.getElementsByTagName("defName")[0].childNodes[0].data)
            data["label"].append(element.getElementsByTagName("label")[0].childNodes[0].data)
            data["desc"].append(element.getElementsByTagName("description")[0].childNodes[0].data)

def scrape_resources(files):
    resources = []
    for file in files:
        debug(f"checking {file} for resources...")
        dom = xml.parse(file)
        for costList in dom.getElementsByTagName("costList"):
            for resource in costList.childNodes:
                if resource.nodeType == xml.Node.ELEMENT_NODE:
                    resources.append(resource.tagName)
    return set(resources)

def scrape_files_for_promo(out_path):
    data = {"file_name": [], "type": [], "name": [], "label": [], "desc": [], "texPath": []}
    files = all_files_in_dir()
    resources = scrape_resources(files)
    debug("found resources: " + str(resources))
    for resourceName in resources:
        data[resourceName] = []
    for file in files:
        scrape_file_for_promo(data, file, resources)
    pd.DataFrame(data).to_csv(out_path)
    debug(f'data output to {out_path}')

def scrape_file_for_promo(data, file, resources):
    debug(f'processing file {file}')
    dom = getChildrenByTagName(xml.parse(file), "Defs")[0]
    elements = getChildrenByTagName(dom, "ThingDef")
    debug(f'found {len(elements)} nodes to process')
    for element in elements:
        if not element.hasAttribute("Abstract"):
            data["file_name"].append(file)
            data["type"].append(element.tagName)
            data["name"].append(element.getElementsByTagName("defName")[0].childNodes[0].data)
            data["label"].append(element.getElementsByTagName("label")[0].childNodes[0].data)
            desc = ""
            descElements = element.getElementsByTagName("description")
            if len(descElements) != 0:
                desc = descElements[0].childNodes[0].data
            data["desc"].append(desc)
            texPath = ""
            texPathElements = element.getElementsByTagName("texPath")
            if len(texPathElements) != 0:
                texPath = element.getElementsByTagName("texPath")[0].childNodes[0].data
            data["texPath"].append(texPath)
            resourcesNotSet = resources.copy()
            costList = []
            costListElements = element.getElementsByTagName("costList")
            if len(costListElements) != 0:
                costList = costListElements[0].childNodes
            for resource in costList:
                if resource.nodeType == xml.Node.ELEMENT_NODE:
                    data[resource.tagName].append(resource.childNodes[0].data)
                    resourcesNotSet.remove(resource.tagName)
            for resource in resourcesNotSet:
                data[resource].append(0)


if __name__ == "__main__":
    scrape_files_for_promo("promo.csv")
