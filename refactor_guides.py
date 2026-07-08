import os
import glob
import re

def process_vue_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Step 1: Remove v-if="false" that was added previously
    content = re.sub(r'(<!-- Guide[^\n]*-->\s*<div )v-if="false" ', r'\1', content)

    # Step 2: Find the guide block
    pattern = r'(<!-- Guide[^\n]*-->\s*)<div\b'
    match = re.search(pattern, content)
    if not match:
        if '<ModuleGuide' not in content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
        return False

    start_idx = match.end() - 4 # start of <div
    
    # Count divs to find the matching closing div
    div_count = 0
    i = start_idx
    end_idx = -1
    while i < len(content):
        if content.startswith('<div', i):
            div_count += 1
            i += 4
        elif content.startswith('</div', i):
            div_count -= 1
            if div_count == 0:
                end_idx = i + 6 # include </div>
                break
            i += 6
        else:
            i += 1
            
    if end_idx == -1:
        return False
        
    guide_block = content[start_idx:end_idx]
    
    # Extract title
    title_match = re.search(r'<h4[^>]*>(.*?)</h4>', guide_block)
    title = title_match.group(1).strip() if title_match else '模組指南'
    
    if title_match:
        parts = guide_block[title_match.end():].rsplit('</div>', 2)
        if len(parts) >= 3:
            inner_content = parts[0].strip()
        else:
            inner_content = guide_block[title_match.end():].strip()
    else:
        # If no h4, it's a simple guide tip like in TrendChartView
        # Just grab the text inside the span or the inner text
        span_match = re.search(r'<span[^>]*>(.*?)</span>', guide_block, re.DOTALL)
        if span_match:
            inner_content = span_match.group(1).strip()
        else:
            inner_content = guide_block
            inner_content = re.sub(r'^<div[^>]*>', '', inner_content)
            inner_content = re.sub(r'</div>$', '', inner_content).strip()
            
    new_guide_block = f'<ModuleGuide title="{title}">\n      {inner_content}\n    </ModuleGuide>'
    
    new_content = content[:start_idx] + new_guide_block + content[end_idx:]
    
    # Add import ModuleGuide if not present
    if 'import ModuleGuide' not in new_content:
        new_content = re.sub(
            r'(<script setup>.*?)(import [^\n]+;)', 
            r'\1import ModuleGuide from "../components/ModuleGuide.vue";\n\2', 
            new_content, count=1, flags=re.DOTALL
        )
        
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
    return True

directory = r'd:\SPC\frontend\mes-spc-web\src\views'
files = glob.glob(os.path.join(directory, '*.vue'))
count = 0
for f in files:
    try:
        if process_vue_file(f):
            print(f"Refactored {os.path.basename(f)}")
            count += 1
    except Exception as e:
        print(f"Failed to process {os.path.basename(f)}: {e}")
        
print(f"Total files refactored: {count}")
