package demo;

import java.util.Arrays;

/**
 * BuggyExample - classroom example with intentional simple programming mistakes.
 *
 * SpotBugs will detect these issues in the compiled bytecode and the
 * spotbugs:check goal will FAIL the build.
 */
public class BuggyExample {

    private String studentName;
    private int[] grades;

    public BuggyExample(String studentName, int[] grades) {
        this.studentName = studentName;
        // BUG: EI_EXPOSE_REP2 - stores external mutable array directly
        this.grades = grades;
    }

    // BUG: EI_EXPOSE_REP - returns internal mutable array directly
    public int[] getGrades() {
        return grades;
    }

    // BUG: NP_NULL_ON_SOME_PATH - dereferences after null check
    public String formatStudentName(String prefix) {
        if (prefix == null) {
            System.out.println("No prefix provided");
        }
        // prefix may be null here
        return prefix.trim() + " " + studentName;
    }

    // BUG: ES_COMPARING_STRINGS_WITH_EQ - string comparison with ==
    public boolean hasHonorsTitle() {
        String title = getTitle();
        // comparing strings with == instead of .equals()
        return title == "Honors";
    }

    // BUG: HE_EQUALS_NO_HASHCODE - equals without hashCode
    @Override
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null || getClass() != obj.getClass()) return false;
        BuggyExample other = (BuggyExample) obj;
        return studentName != null
                ? studentName.equals(other.studentName)
                : other.studentName == null;
    }
    // Missing hashCode()!

    private String getTitle() {
        return "Honors";
    }

    public String getStudentName() {
        return studentName;
    }

    public static void main(String[] args) {
        int[] grades = {85, 90, 78};
        BuggyExample student = new BuggyExample("Alice", grades);
        System.out.println("Student: " + student.getStudentName());
        System.out.println("Grades: " + Arrays.toString(student.getGrades()));
        System.out.println("Has honors: " + student.hasHonorsTitle());
    }
}
