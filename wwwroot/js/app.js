// Safari兼容性修复和通用工具函数

// 文件下载功能（Safari兼容）
window.downloadFile = function(filename, base64Data) {
    try {
        // 解码base64数据
        const binaryString = atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        // 创建Blob对象
        const blob = new Blob([bytes], { type: 'text/csv;charset=utf-8;' });

        // Safari特殊处理
        if (window.navigator && window.navigator.msSaveOrOpenBlob) {
            // IE/Edge
            window.navigator.msSaveOrOpenBlob(blob, filename);
        } else {
            // 现代浏览器
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            link.style.display = 'none';
            
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            // 清理URL对象
            setTimeout(() => {
                window.URL.revokeObjectURL(url);
            }, 100);
        }
    } catch (error) {
        console.error('下载文件失败:', error);
        alert('下载文件失败，请重试');
    }
};

// 复制到剪贴板功能（Safari兼容）
window.copyToClipboard = function(text) {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            // 现代浏览器
            return navigator.clipboard.writeText(text);
        } else {
            // 老版本浏览器fallback
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            
            const result = document.execCommand('copy');
            document.body.removeChild(textArea);
            
            if (result) {
                return Promise.resolve();
            } else {
                return Promise.reject(new Error('复制失败'));
            }
        }
    } catch (error) {
        console.error('复制失败:', error);
        return Promise.reject(error);
    }
};

// 检查浏览器兼容性
window.checkBrowserCompatibility = function() {
    const isIE = /MSIE|Trident/.test(navigator.userAgent);
    const isOldSafari = /Safari/.test(navigator.userAgent) && /Version\/[0-9]\./.test(navigator.userAgent);
    
    if (isIE) {
        console.warn('检测到IE浏览器，某些功能可能不可用');
        return false;
    }
    
    if (isOldSafari) {
        console.warn('检测到较旧的Safari版本，建议更新浏览器');
    }
    
    return true;
};

// 初始化应用
window.initializeApp = function() {
    console.log('智能补货计算系统初始化...');
    
    // 检查浏览器兼容性
    const isCompatible = checkBrowserCompatibility();
    
    if (!isCompatible) {
        console.error('浏览器兼容性检查失败');
    }
    
    // 添加全局错误处理
    window.addEventListener('error', function(event) {
        console.error('JavaScript错误:', event.error);
    });
    
    window.addEventListener('unhandledrejection', function(event) {
        console.error('未处理的Promise拒绝:', event.reason);
    });
    
    console.log('应用初始化完成');
};

// 当DOM加载完成时初始化
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeApp);
} else {
    initializeApp();
}

async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
} 