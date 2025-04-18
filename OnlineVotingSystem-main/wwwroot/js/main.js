document.addEventListener("DOMContentLoaded", function () {
    console.log("JS Loaded! ✅");

    const registerForm = document.getElementById("registerForm");
    if (registerForm) {
        registerForm.addEventListener("submit", async function (event) {
            event.preventDefault();
            console.log("Register button clicked! ✅");
    
            const username = document.getElementById("username")?.value;
            const email = document.getElementById("email")?.value;
            const password = document.getElementById("password")?.value;
    
            // const encryptionType = "RSA"; // Fixed encryption type
            const encryptionTypes = ["AES", "RSA", "Blowfish"];
            const encryptionType = encryptionTypes[Math.floor(Math.random() * encryptionTypes.length)];
    
            const userData = {
                username,
                email,
                passwordHash: password,
                encryptionType
            };
    
            if (!username || !email || !password) {
                alert("❌ Please fill all fields!");
                return;
            }
    
            try {
                // Step 1: Register User
                const registerResponse = await fetch("http://localhost:5019/api/auth/register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(userData)
                });
    
                const registerResult = await registerResponse.json();
                console.log("Register Response:", registerResult);
    
                if (!registerResponse.ok) {
                    alert("Registration failed: " + (registerResult.message || "Unknown error"));
                    return;
                }
    
                // Step 2: Fetch CAPTCHA ZIP (contains both raw & encrypted)
                const captchaZipResponse = await fetch(`http://localhost:5019/api/auth/generateCaptcha?email=${email}`);
                if (!captchaZipResponse.ok) {
                    alert("Failed to generate CAPTCHA bundle.");
                    return;
                }
    
                const zipBlob = await captchaZipResponse.blob();
    
                // Use JSZip to extract the ZIP content
                const zip = await JSZip.loadAsync(zipBlob);
    
                // Extract files from ZIP
                const rawCaptchaBlob = await zip.file("captcha_raw.png").async("blob");
                const encryptedHalfBlob = await zip.file("captcha_half.png").async("blob");
    
                // Step 3: Download raw immediately
                const rawUrl = URL.createObjectURL(rawCaptchaBlob);
                const rawLink = document.createElement("a");
                rawLink.href = rawUrl;
                rawLink.download = "captcha_raw.png";
                rawLink.click();
                console.log("✅ Raw CAPTCHA downloaded!");
    
                // Step 4: Create download button for encrypted half
                const encryptedUrl = URL.createObjectURL(encryptedHalfBlob);
                let downloadButton = document.getElementById("downloadCaptchaButton");
                if (!downloadButton) {
                    downloadButton = document.createElement("button");
                    downloadButton.textContent = "Download CAPTCHA";
                    downloadButton.className = "btn btn-success mt-3";
                    downloadButton.id = "downloadCaptchaButton";
                    registerForm.appendChild(downloadButton);
                }
    
                // Step 5: Handle encrypted CAPTCHA download
                downloadButton.onclick = () => {
                    const encLink = document.createElement("a");
                    encLink.href = encryptedUrl;
                    encLink.download = "captcha_half.png";
                    encLink.click();
                    
                    // Instead of reloading the page, clear the form to prevent re-fetching
                    registerForm.reset(); 
                    downloadButton.disabled = true; // Prevent duplicate downloads

                    setTimeout(() => {
                        window.location.href = "index.html"; // Redirect after download
                    }, 2000);
                };
    
                alert(registerResult.message + " ✅ CAPTCHA generated. Please download your encrypted half.");
    
            } catch (error) {
                console.error("❌ ERROR:", error);
                alert("Server error, please try again.");
            }
        });
    }

    // ✅ Handle Login (Only if found)
    const loginForm = document.getElementById("loginForm");
    if (loginForm) {
        loginForm.addEventListener("submit", async function (event) {
            event.preventDefault();
            console.log("Login button clicked! ✅");

            const email = document.getElementById("loginEmail")?.value;
            const password = document.getElementById("loginPassword")?.value;
            const captchaFileInput = document.getElementById("captchaFile");
            const captchaFile = captchaFileInput?.files?.[0];

            if (!email || !password || !captchaFile) {
                alert("❌ Please fill all fields and upload your CAPTCHA half!");
                return;
            }

            // ✅ Use FormData to handle file + fields
            const formData = new FormData();
            formData.append("email", email);
            formData.append("password", password);
            formData.append("captchaHalf", captchaFile); // 👈 backend should use [FromForm] IFormFile

            try {
                const response = await fetch("http://localhost:5019/api/auth/login", {
                    method: "POST",
                    body: formData // 👈 don't set headers, browser sets correct multipart boundary
                });

                const result = await response.json();
                console.log("Login Response:", result);

                if (response.ok) {
                    alert(result.message);
                    localStorage.setItem("userId", result.userId);
                    window.location.href = "voting.html";
                } else {
                    alert("Login failed: " + (result.message || "Invalid credentials"));
                }
            } catch (error) {
                console.error("❌ ERROR:", error);
                alert("Server error, please try again.");
            }
        });
    } else {
        console.warn("⚠ No login form found. (You may be on register.html)");
    }


    // ✅ Voting Page Logic
    if (window.location.pathname.includes("voting.html")) {
        console.log("Voting Page Loaded! ✅");

        const userId = localStorage.getItem("userId");
        if (!userId) {
            alert("❌ You must log in first!");
            window.location.href = "login.html";
            return;
        }

        // Prevent voting twice
        const hasVoted = localStorage.getItem("hasVoted");
        if (hasVoted) {
            document.getElementById("voteMessage").innerText = "✅ You have already voted!";
            document.getElementById("voteForm").style.display = "none";
        }

        document.getElementById("voteForm").addEventListener("submit", async function (event) {
            event.preventDefault();
            console.log("Vote button clicked! ✅");

            const candidateId = document.querySelector('input[name="candidate"]:checked')?.value;
            if (!candidateId) {
                alert("❌ Please select a candidate!");
                return;
            }

            try {
                const response = await fetch("http://localhost:5019/api/vote", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ userId, candidateId })
                });

                const result = await response.json();
                console.log("Vote Response:", result);

                if (response.ok) {
                    alert("✅ Vote submitted successfully!");
                    localStorage.setItem("hasVoted", "true");
                    document.getElementById("voteMessage").innerText = "✅ You have already voted!";
                    document.getElementById("voteForm").style.display = "none";
                } else {
                    alert("❌ Voting failed: " + (result.message || "Error occurred"));
                }
            } catch (error) {
                console.error("❌ Fetch Error:", error);
                alert("❌ Server error, please try again.");
            }
        });
    }
});
