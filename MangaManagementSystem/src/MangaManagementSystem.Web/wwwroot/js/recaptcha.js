window.recaptchaInterop = {
    loadScript: function () {
        return new Promise((resolve, reject) => {
            if (typeof grecaptcha !== 'undefined' && grecaptcha.render) {
                resolve();
                return;
            }
            var existingScript = document.querySelector('script[src*="recaptcha/api.js"]');
            if (existingScript) {
                var checkInterval = setInterval(() => {
                    if (typeof grecaptcha !== 'undefined' && grecaptcha.render) {
                        clearInterval(checkInterval);
                        resolve();
                    }
                }, 50);
                return;
            }

            var script = document.createElement('script');
            script.src = 'https://www.google.com/recaptcha/api.js?render=explicit';
            script.async = true;
            script.defer = true;
            script.onload = () => {
                var checkInterval = setInterval(() => {
                    if (typeof grecaptcha !== 'undefined' && grecaptcha.render) {
                        clearInterval(checkInterval);
                        resolve();
                    }
                }, 50);
            };
            script.onerror = (err) => {
                reject(err);
            };
            document.head.appendChild(script);
        });
    },
    render: function (elementId, siteKey) {
        return this.loadScript().then(() => {
            if (typeof grecaptcha !== 'undefined' && grecaptcha.render) {
                try {
                    var element = document.getElementById(elementId);
                    if (element) {
                        if (element.querySelector('iframe') || element.querySelector('.grecaptcha-logo')) {
                            return -1;
                        }
                        element.innerHTML = ""; // Clear existing contents
                        return grecaptcha.render(elementId, {
                            'sitekey': siteKey
                        });
                    }
                } catch (e) {
                    console.error("reCAPTCHA render error: ", e);
                }
            }
            return -1;
        }).catch(err => {
            console.error("reCAPTCHA script load failed: ", err);
            return -1;
        });
    },
    getResponse: function (widgetId) {
        if (typeof grecaptcha !== 'undefined') {
            if (widgetId !== undefined && widgetId !== null) {
                return grecaptcha.getResponse(widgetId);
            }
            return grecaptcha.getResponse();
        }
        return "";
    },
    reset: function (widgetId) {
        if (typeof grecaptcha !== 'undefined') {
            if (widgetId !== undefined && widgetId !== null) {
                grecaptcha.reset(widgetId);
            } else {
                grecaptcha.reset();
            }
        }
    }
};
